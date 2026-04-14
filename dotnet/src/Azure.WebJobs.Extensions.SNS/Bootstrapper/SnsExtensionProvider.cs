// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Azure.WebJobs.Extensions.SNS;

using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService.Model;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Extension configuration provider for Amazon SNS bindings.
/// Implements IAsyncConverter to handle incoming webhooks via /runtime/webhooks/sns
/// </summary>
[Extension(name: "sns", configurationSection: "sns")]
public class SnsExtensionProvider : IExtensionConfigProvider, 
    IAsyncConverter<HttpRequestMessage, HttpResponseMessage>
{
    private readonly INameResolver _nameResolver;
    private readonly ILoggerFactory _loggerFactory;
    private readonly SnsSignatureValidator _signatureValidator;
    private readonly SnsWebhookHandler _webhookHandler;
    private readonly IOptions<SnsWebhookOptions> _webhookOptions;
    private Uri? _webhookUrl;

    public SnsExtensionProvider(
        INameResolver nameResolver, 
        ILoggerFactory loggerFactory,
        IHttpClientFactory? httpClientFactory = null,
        IOptions<SnsWebhookOptions>? webhookOptions = null)
    {
        _nameResolver = nameResolver ?? throw new ArgumentNullException(nameof(nameResolver));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _webhookOptions = webhookOptions ?? Options.Create(new SnsWebhookOptions());
        
        // Use IHttpClientFactory if available, otherwise create a shared HttpClient
        var httpClient = httpClientFactory?.CreateClient("SNS") ?? new HttpClient();
        var logger = loggerFactory.CreateLogger<SnsSignatureValidator>();
        _signatureValidator = new SnsSignatureValidator(httpClient, logger);
        _webhookHandler = new SnsWebhookHandler(
            _signatureValidator, 
            _webhookOptions, 
            _loggerFactory,
            httpClientFactory);
    }

    /// <summary>
    /// Gets the webhook URL where SNS should send notifications.
    /// Available after Initialize() is called.
    /// </summary>
    public Uri? WebhookUrl => _webhookUrl;

    /// <summary>
    /// Gets the webhook handler for registering function handlers.
    /// </summary>
    internal SnsWebhookHandler WebhookHandler => _webhookHandler;

    /// <summary>
    /// Initializes the SNS extension with the Azure Functions host.
    /// </summary>
    public void Initialize(ExtensionConfigContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Register this extension as a webhook handler
        // This exposes the endpoint at /runtime/webhooks/sns?code={key}
#pragma warning disable CS0618 // Type or member is obsolete
        _webhookUrl = context.GetWebhookHandler();
#pragma warning restore CS0618

        var logger = _loggerFactory.CreateLogger<SnsExtensionProvider>();
        if (_webhookUrl != null)
        {
            logger.LogInformation("SNS webhook endpoint registered at: {WebhookUrl}", _webhookUrl);
        }
        else
        {
            logger.LogWarning("SNS webhook endpoint could not be registered. IWebHookProvider not available.");
        }

        // Register trigger binding
        var triggerRule = context.AddBindingRule<SnsTriggerAttribute>();
        triggerRule.BindToTrigger(new SnsTriggerBindingProvider(
            _loggerFactory, 
            _nameResolver, 
            _signatureValidator,
            _webhookHandler));

        // Register output binding
        var outputRule = context.AddBindingRule<SnsOutAttribute>();
        outputRule.BindToCollector(attribute => new SnsAsyncCollector(attribute));

        // Add converters
        outputRule.AddConverter<SnsMessage, PublishRequest>(ConvertMessageToRequest);
        outputRule.AddConverter<string, PublishRequest>(ConvertStringToRequest);
    }

    /// <summary>
    /// Handles incoming HTTP requests from SNS.
    /// This is called by the Azure Functions host when requests arrive at /runtime/webhooks/sns
    /// </summary>
    public Task<HttpResponseMessage> ConvertAsync(HttpRequestMessage input, CancellationToken cancellationToken)
    {
        return _webhookHandler.ConvertAsync(input, cancellationToken);
    }

    private static PublishRequest ConvertMessageToRequest(SnsMessage message)
    {
        var request = new PublishRequest
        {
            TopicArn = message.TopicArn,
            Message = message.Message,
            Subject = message.Subject,
            MessageGroupId = message.MessageGroupId,
            MessageDeduplicationId = message.MessageDeduplicationId
        };

        if (message.MessageAttributes != null)
        {
            foreach (var attr in message.MessageAttributes)
            {
                request.MessageAttributes[attr.Key] = new Amazon.SimpleNotificationService.Model.MessageAttributeValue
                {
                    DataType = attr.Value.DataType,
                    StringValue = attr.Value.StringValue,
                    BinaryValue = attr.Value.BinaryValue != null 
                        ? new MemoryStream(attr.Value.BinaryValue) 
                        : null
                };
            }
        }

        return request;
    }

    private static PublishRequest ConvertStringToRequest(string message)
    {
        return new PublishRequest
        {
            Message = message
        };
    }
}
