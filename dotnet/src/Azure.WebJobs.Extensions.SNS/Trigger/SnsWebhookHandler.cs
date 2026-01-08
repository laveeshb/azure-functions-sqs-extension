// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Azure.WebJobs.Extensions.SNS;

using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Handles incoming SNS webhook requests via the Azure Functions webhook infrastructure.
/// Implements IAsyncConverter to integrate with the /runtime/webhooks/sns endpoint.
/// </summary>
public class SnsWebhookHandler : IAsyncConverter<HttpRequestMessage, HttpResponseMessage>
{
    private readonly SnsSignatureValidator _signatureValidator;
    private readonly SnsWebhookOptions _options;
    private readonly ILogger _logger;
    private readonly IHttpClientFactory? _httpClientFactory;

    // Registered listeners by ID
    private readonly ConcurrentDictionary<string, SnsTriggerListener> _listeners = new();

    public SnsWebhookHandler(
        SnsSignatureValidator signatureValidator,
        IOptions<SnsWebhookOptions> options,
        ILoggerFactory loggerFactory,
        IHttpClientFactory? httpClientFactory = null)
    {
        _signatureValidator = signatureValidator ?? throw new ArgumentNullException(nameof(signatureValidator));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = loggerFactory?.CreateLogger<SnsWebhookHandler>() ?? throw new ArgumentNullException(nameof(loggerFactory));
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Registers a listener to receive notifications.
    /// </summary>
    public void RegisterHandler(string handlerId, SnsTriggerListener listener)
    {
        _listeners[handlerId] = listener;
        _logger.LogDebug("Registered SNS listener: {HandlerId}", handlerId);
    }

    /// <summary>
    /// Unregisters a listener.
    /// </summary>
    public void UnregisterHandler(string handlerId)
    {
        _listeners.TryRemove(handlerId, out _);
        _logger.LogDebug("Unregistered SNS listener: {HandlerId}", handlerId);
    }

    /// <summary>
    /// Handles incoming HTTP requests from SNS.
    /// Called by the Azure Functions host via /runtime/webhooks/sns
    /// </summary>
    public async Task<HttpResponseMessage> ConvertAsync(HttpRequestMessage input, CancellationToken cancellationToken)
    {
        if (input.Method != HttpMethod.Post)
        {
            _logger.LogWarning("Received non-POST request: {Method}", input.Method);
            return new HttpResponseMessage(HttpStatusCode.MethodNotAllowed);
        }

        // Read the request body
        if (input.Content == null)
        {
            _logger.LogWarning("Received empty request body");
            return new HttpResponseMessage(HttpStatusCode.BadRequest);
        }

        var body = await input.Content.ReadAsStringAsync();
        if (string.IsNullOrEmpty(body))
        {
            _logger.LogWarning("Received empty request body");
            return new HttpResponseMessage(HttpStatusCode.BadRequest);
        }

        // Parse the SNS notification
        SnsNotification? notification;
        try
        {
            notification = JsonSerializer.Deserialize<SnsNotification>(body);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse SNS notification");
            return new HttpResponseMessage(HttpStatusCode.BadRequest);
        }

        if (notification == null)
        {
            _logger.LogWarning("Parsed notification is null");
            return new HttpResponseMessage(HttpStatusCode.BadRequest);
        }

        // Handle different message types
        return notification.Type switch
        {
            "SubscriptionConfirmation" => await HandleSubscriptionConfirmationAsync(notification, cancellationToken),
            "Notification" => await HandleNotificationAsync(notification, cancellationToken),
            "UnsubscribeConfirmation" => HandleUnsubscribeConfirmation(notification),
            _ => HandleUnknownMessageType(notification)
        };
    }

    private async Task<HttpResponseMessage> HandleSubscriptionConfirmationAsync(
        SnsNotification notification,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received subscription confirmation for topic {TopicArn}",
            notification.TopicArn);

        // Find listeners interested in this topic
        var matchingListeners = GetMatchingListeners(notification.TopicArn);
        
        if (!matchingListeners.Any())
        {
            _logger.LogWarning("No listeners registered for topic {TopicArn}", notification.TopicArn);
            
            // Still auto-confirm if global setting is enabled
            if (_options.AutoConfirmSubscription && !string.IsNullOrEmpty(notification.SubscribeUrl))
            {
                return await ConfirmSubscriptionAsync(notification, cancellationToken);
            }
            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        // Let each matching listener confirm if they want to
        foreach (var listener in matchingListeners)
        {
            await listener.ConfirmSubscriptionAsync(notification, cancellationToken);
        }

        return new HttpResponseMessage(HttpStatusCode.OK);
    }

    private async Task<HttpResponseMessage> ConfirmSubscriptionAsync(
        SnsNotification notification,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(notification.SubscribeUrl))
        {
            return new HttpResponseMessage(HttpStatusCode.BadRequest);
        }

        try
        {
            var httpClient = _httpClientFactory?.CreateClient("SNS") ?? new HttpClient();
            var response = await httpClient.GetAsync(notification.SubscribeUrl, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully confirmed subscription to topic {TopicArn}", notification.TopicArn);
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
            else
            {
                _logger.LogError("Failed to confirm subscription. Status: {StatusCode}", response.StatusCode);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming subscription to topic {TopicArn}", notification.TopicArn);
            return new HttpResponseMessage(HttpStatusCode.InternalServerError);
        }
    }

    private async Task<HttpResponseMessage> HandleNotificationAsync(
        SnsNotification notification,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Processing SNS notification {MessageId} from topic {TopicArn}",
            notification.MessageId,
            notification.TopicArn);

        // Find listeners interested in this topic
        var matchingListeners = GetMatchingListeners(notification.TopicArn);

        if (!matchingListeners.Any())
        {
            _logger.LogWarning("No listeners registered for topic {TopicArn}", notification.TopicArn);
            return new HttpResponseMessage(HttpStatusCode.OK); // ACK anyway to prevent retries
        }

        // Verify signature once (if any listener requires it)
        var requiresSignature = matchingListeners.Any(l => l.RequireSignatureVerification);
        if (requiresSignature)
        {
            var isValid = await _signatureValidator.ValidateSignatureAsync(notification, cancellationToken);
            if (!isValid)
            {
                _logger.LogWarning("SNS signature verification failed for message {MessageId}", notification.MessageId);
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }
        }

        // Dispatch to all matching listeners
        var anyFailed = false;
        var anyHandled = false;

        foreach (var listener in matchingListeners)
        {
            try
            {
                var result = await listener.HandleNotificationAsync(notification, cancellationToken);
                
                switch (result)
                {
                    case SnsHandlerResult.Success:
                        anyHandled = true;
                        break;
                    case SnsHandlerResult.Failed:
                        anyFailed = true;
                        break;
                    // Skipped doesn't affect outcome
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in listener for message {MessageId}", notification.MessageId);
                anyFailed = true;
            }
        }

        if (!anyHandled)
        {
            _logger.LogDebug("No listeners handled message {MessageId}", notification.MessageId);
        }

        if (anyFailed && _options.RetryOnFailure)
        {
            return new HttpResponseMessage(HttpStatusCode.InternalServerError);
        }

        return new HttpResponseMessage(HttpStatusCode.OK);
    }

    private IEnumerable<SnsTriggerListener> GetMatchingListeners(string? topicArn)
    {
        foreach (var listener in _listeners.Values)
        {
            // If listener has no topic filter, it matches all
            if (string.IsNullOrEmpty(listener.TopicArnFilter))
            {
                yield return listener;
            }
            // If listener has topic filter, it must match
            else if (listener.TopicArnFilter == topicArn)
            {
                yield return listener;
            }
        }
    }

    private HttpResponseMessage HandleUnsubscribeConfirmation(SnsNotification notification)
    {
        _logger.LogInformation(
            "Received unsubscribe confirmation for topic {TopicArn}",
            notification.TopicArn);
        return new HttpResponseMessage(HttpStatusCode.OK);
    }

    private HttpResponseMessage HandleUnknownMessageType(SnsNotification notification)
    {
        _logger.LogWarning("Unknown SNS message type: {Type}", notification.Type);
        return new HttpResponseMessage(HttpStatusCode.BadRequest);
    }
}

/// <summary>
/// Configuration options for the SNS webhook handler.
/// </summary>
public class SnsWebhookOptions
{
    /// <summary>
    /// Whether to automatically confirm subscription requests. Default is true.
    /// </summary>
    public bool AutoConfirmSubscription { get; set; } = true;

    /// <summary>
    /// Whether to return 500 on function failures to trigger SNS retries.
    /// Default is true.
    /// </summary>
    public bool RetryOnFailure { get; set; } = true;
}
