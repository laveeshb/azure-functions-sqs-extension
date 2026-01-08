// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Azure.WebJobs.Extensions.SNS;

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Extensions.Logging;

/// <summary>
/// Listener that receives SNS notifications via the centralized webhook handler.
/// Registers with SnsWebhookHandler to receive notifications matching its topic/subject filters.
/// </summary>
public class SnsTriggerListener : IListener
{
    private readonly SnsTriggerAttribute _attribute;
    private readonly ITriggeredFunctionExecutor _executor;
    private readonly ILogger _logger;
    private readonly SnsSignatureValidator _signatureValidator;
    private readonly SnsWebhookHandler _webhookHandler;
    private readonly FunctionDescriptor _functionDescriptor;
    private readonly HttpClient _httpClient;
    private readonly string _handlerId;
    private bool _disposed;

    public SnsTriggerListener(
        SnsTriggerAttribute attribute,
        ITriggeredFunctionExecutor executor,
        ILoggerFactory loggerFactory,
        SnsSignatureValidator signatureValidator,
        SnsWebhookHandler webhookHandler,
        FunctionDescriptor functionDescriptor)
    {
        _attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _signatureValidator = signatureValidator ?? throw new ArgumentNullException(nameof(signatureValidator));
        _webhookHandler = webhookHandler ?? throw new ArgumentNullException(nameof(webhookHandler));
        _functionDescriptor = functionDescriptor ?? throw new ArgumentNullException(nameof(functionDescriptor));
        _logger = loggerFactory?.CreateLogger<SnsTriggerListener>() ?? throw new ArgumentNullException(nameof(loggerFactory));
        _httpClient = new HttpClient();
        _handlerId = Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Handles an incoming SNS notification that was dispatched by the webhook handler.
    /// </summary>
    internal async Task<SnsHandlerResult> HandleNotificationAsync(
        SnsNotification notification,
        CancellationToken cancellationToken)
    {
        // Apply subject filter if specified
        if (!string.IsNullOrEmpty(_attribute.SubjectFilter) &&
            !string.Equals(notification.Subject, _attribute.SubjectFilter, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug(
                "Message subject '{Subject}' does not match filter '{Filter}', skipping",
                notification.Subject,
                _attribute.SubjectFilter);
            return SnsHandlerResult.Skipped;
        }

        _logger.LogDebug(
            "Processing SNS notification {MessageId} from topic {TopicArn}",
            notification.MessageId,
            notification.TopicArn);

        try
        {
            var triggerData = new TriggeredFunctionData
            {
                TriggerValue = notification
            };

            var result = await _executor.TryExecuteAsync(triggerData, cancellationToken);

            if (result.Succeeded)
            {
                _logger.LogDebug("Successfully processed message {MessageId}", notification.MessageId);
                return SnsHandlerResult.Success;
            }
            else
            {
                _logger.LogError(
                    result.Exception,
                    "Failed to process message {MessageId}",
                    notification.MessageId);
                return SnsHandlerResult.Failed;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing message {MessageId}", notification.MessageId);
            return SnsHandlerResult.Failed;
        }
    }

    /// <summary>
    /// Confirms an SNS subscription by calling the SubscribeURL.
    /// </summary>
    internal async Task<bool> ConfirmSubscriptionAsync(
        SnsNotification notification,
        CancellationToken cancellationToken)
    {
        if (!_attribute.AutoConfirmSubscription)
        {
            _logger.LogInformation("Auto-confirm disabled for function {FunctionName}", _functionDescriptor.ShortName);
            return true;
        }

        if (string.IsNullOrEmpty(notification.SubscribeUrl))
        {
            _logger.LogWarning("SubscribeURL is missing from subscription confirmation");
            return false;
        }

        try
        {
            var response = await _httpClient.GetAsync(notification.SubscribeUrl, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Successfully confirmed subscription to topic {TopicArn}",
                    notification.TopicArn);
                return true;
            }
            else
            {
                _logger.LogError(
                    "Failed to confirm subscription. Status: {StatusCode}",
                    response.StatusCode);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming subscription to topic {TopicArn}", notification.TopicArn);
            return false;
        }
    }

    /// <summary>
    /// Gets the topic ARN filter for this listener (can be null for all topics).
    /// </summary>
    internal string? TopicArnFilter => _attribute.TopicArn;

    /// <summary>
    /// Gets whether signature verification is required.
    /// </summary>
    internal bool RequireSignatureVerification => _attribute.VerifySignature;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Register this listener with the centralized webhook handler
        _webhookHandler.RegisterHandler(_handlerId, this);

        _logger.LogInformation(
            "SNS trigger listener started for function {FunctionName}. " +
            "Webhook URL: /runtime/webhooks/sns?code={{function_key}}",
            _functionDescriptor.ShortName);
        
        if (!string.IsNullOrEmpty(_attribute.TopicArn))
        {
            _logger.LogInformation("Filtering for topic: {TopicArn}", _attribute.TopicArn);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        // Unregister from the webhook handler
        _webhookHandler.UnregisterHandler(_handlerId);

        _logger.LogInformation(
            "SNS trigger listener stopping for function {FunctionName}",
            _functionDescriptor.ShortName);
        return Task.CompletedTask;
    }

    public void Cancel()
    {
        // No-op for HTTP-based listener
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _webhookHandler.UnregisterHandler(_handlerId);
        _httpClient.Dispose();
        _disposed = true;
    }
}

/// <summary>
/// Result of processing an SNS notification.
/// </summary>
public enum SnsHandlerResult
{
    /// <summary>Message was processed successfully.</summary>
    Success,
    /// <summary>Message was skipped (didn't match filter).</summary>
    Skipped,
    /// <summary>Message processing failed.</summary>
    Failed
}
