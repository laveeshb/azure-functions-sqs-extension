// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Azure.WebJobs.Extensions.SNS;

using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

/// <summary>
/// Validates SNS message signatures to ensure messages are authentically from AWS SNS.
/// </summary>
public class SnsSignatureValidator
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<string, X509Certificate2> _certificateCache = new();
    private static readonly TimeSpan CertificateCacheExpiry = TimeSpan.FromHours(1);

    // Valid AWS SNS signing certificate URL patterns
    private static readonly string[] ValidCertificateDomains = new[]
    {
        ".amazonaws.com",
        ".amazonaws.com.cn"
    };

    public SnsSignatureValidator(HttpClient httpClient, ILogger logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validates an SNS message signature.
    /// </summary>
    /// <param name="notification">The SNS notification to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the signature is valid, false otherwise.</returns>
    public async Task<bool> ValidateSignatureAsync(SnsNotification notification, CancellationToken cancellationToken = default)
    {
        if (notification == null)
        {
            _logger.LogWarning("Cannot validate null notification");
            return false;
        }

        if (string.IsNullOrEmpty(notification.SigningCertUrl))
        {
            _logger.LogWarning("Notification has no SigningCertURL");
            return false;
        }

        if (string.IsNullOrEmpty(notification.Signature))
        {
            _logger.LogWarning("Notification has no Signature");
            return false;
        }

        // Validate the certificate URL is from AWS
        if (!IsValidCertificateUrl(notification.SigningCertUrl))
        {
            _logger.LogWarning("Invalid SigningCertURL domain: {Url}", notification.SigningCertUrl);
            return false;
        }

        try
        {
            var certificate = await GetCertificateAsync(notification.SigningCertUrl, cancellationToken);
            if (certificate == null)
            {
                _logger.LogWarning("Failed to retrieve signing certificate");
                return false;
            }

            var stringToSign = BuildStringToSign(notification);
            var signature = Convert.FromBase64String(notification.Signature);

            using var rsa = certificate.GetRSAPublicKey();
            if (rsa == null)
            {
                _logger.LogWarning("Certificate does not have RSA public key");
                return false;
            }

            var isValid = rsa.VerifyData(
                Encoding.UTF8.GetBytes(stringToSign),
                signature,
                HashAlgorithmName.SHA1,
                RSASignaturePadding.Pkcs1);

            if (!isValid)
            {
                _logger.LogWarning("SNS signature verification failed for message {MessageId}", notification.MessageId);
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating SNS signature for message {MessageId}", notification.MessageId);
            return false;
        }
    }

    /// <summary>
    /// Validates that the certificate URL is from a trusted AWS domain.
    /// </summary>
    private bool IsValidCertificateUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        // Must be HTTPS
        if (uri.Scheme != Uri.UriSchemeHttps)
            return false;

        // Must be from an AWS domain
        var host = uri.Host.ToLowerInvariant();
        foreach (var domain in ValidCertificateDomains)
        {
            if (host.EndsWith(domain, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Retrieves the signing certificate, using cache if available.
    /// </summary>
    private async Task<X509Certificate2?> GetCertificateAsync(string url, CancellationToken cancellationToken)
    {
        if (_certificateCache.TryGetValue(url, out var cachedCert))
        {
            // Check if certificate is still valid
            if (cachedCert.NotAfter > DateTime.UtcNow)
            {
                return cachedCert;
            }

            _certificateCache.TryRemove(url, out _);
        }

        try
        {
            var pemData = await _httpClient.GetStringAsync(url, cancellationToken);
            var certificate = X509Certificate2.CreateFromPem(pemData);

            // Validate the certificate is from Amazon
            if (!certificate.Subject.Contains("Amazon", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Certificate is not from Amazon: {Subject}", certificate.Subject);
                return null;
            }

            _certificateCache[url] = certificate;
            return certificate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve certificate from {Url}", url);
            return null;
        }
    }

    /// <summary>
    /// Builds the canonical string to sign for SNS signature verification.
    /// The order and format is strictly defined by AWS.
    /// </summary>
    private static string BuildStringToSign(SnsNotification notification)
    {
        var sb = new StringBuilder();

        // The string to sign depends on the message type
        if (notification.Type == "Notification")
        {
            sb.Append("Message\n").Append(notification.Message).Append('\n');
            sb.Append("MessageId\n").Append(notification.MessageId).Append('\n');

            if (!string.IsNullOrEmpty(notification.Subject))
            {
                sb.Append("Subject\n").Append(notification.Subject).Append('\n');
            }

            sb.Append("Timestamp\n").Append(notification.Timestamp).Append('\n');
            sb.Append("TopicArn\n").Append(notification.TopicArn).Append('\n');
            sb.Append("Type\n").Append(notification.Type).Append('\n');
        }
        else // SubscriptionConfirmation or UnsubscribeConfirmation
        {
            sb.Append("Message\n").Append(notification.Message).Append('\n');
            sb.Append("MessageId\n").Append(notification.MessageId).Append('\n');
            sb.Append("SubscribeURL\n").Append(notification.SubscribeUrl).Append('\n');
            sb.Append("Timestamp\n").Append(notification.Timestamp).Append('\n');
            sb.Append("Token\n").Append(notification.Token).Append('\n');
            sb.Append("TopicArn\n").Append(notification.TopicArn).Append('\n');
            sb.Append("Type\n").Append(notification.Type).Append('\n');
        }

        return sb.ToString();
    }
}
