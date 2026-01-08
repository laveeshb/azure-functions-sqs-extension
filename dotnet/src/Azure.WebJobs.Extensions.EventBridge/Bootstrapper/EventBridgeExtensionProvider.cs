// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Azure.WebJobs.Extensions.EventBridge;

using System;
using System.Net.Http;
using Amazon.EventBridge.Model;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Extensions.Logging;

/// <summary>
/// Extension configuration provider for Amazon EventBridge bindings.
/// Registers the output binding and trigger with the Azure Functions host.
/// </summary>
[Extension(name: "eventBridge", configurationSection: "eventBridge")]
public class EventBridgeExtensionProvider : IExtensionConfigProvider
{
    private readonly EventBridgeTriggerBindingProvider _triggerBindingProvider;
    private readonly EventBridgeWebhookHandler _webhookHandler;
    private readonly ILogger _logger;

    public EventBridgeExtensionProvider(
        EventBridgeTriggerBindingProvider triggerBindingProvider,
        EventBridgeWebhookHandler webhookHandler,
        ILoggerFactory loggerFactory)
    {
        _triggerBindingProvider = triggerBindingProvider ?? throw new ArgumentNullException(nameof(triggerBindingProvider));
        _webhookHandler = webhookHandler ?? throw new ArgumentNullException(nameof(webhookHandler));
        _logger = loggerFactory?.CreateLogger<EventBridgeExtensionProvider>() ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    /// <summary>
    /// Initializes the EventBridge extension with the Azure Functions host.
    /// </summary>
    public void Initialize(ExtensionConfigContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _logger.LogInformation("Initializing EventBridge extension");

        // Register the output binding
        var outputRule = context.AddBindingRule<EventBridgeOutAttribute>();
        outputRule.BindToCollector(attribute => new EventBridgeAsyncCollector(attribute));

        // Add converters for common types
        outputRule.AddConverter<EventBridgeMessage, PutEventsRequestEntry>(ConvertMessageToEntry);
        outputRule.AddConverter<string, PutEventsRequestEntry>(ConvertStringToEntry);

        // Register the trigger binding
        var triggerRule = context.AddBindingRule<EventBridgeTriggerAttribute>();
        triggerRule.BindToTrigger(_triggerBindingProvider);

        // Register webhook handler for HTTP-based trigger
        // This allows EventBridge API Destinations to call the function
        context.AddConverter<HttpRequestMessage, HttpResponseMessage>(_webhookHandler);

        _logger.LogInformation("EventBridge extension initialized with output binding and webhook trigger");
    }

    private static PutEventsRequestEntry ConvertMessageToEntry(EventBridgeMessage message)
    {
        return new PutEventsRequestEntry
        {
            EventBusName = message.EventBusName,
            Source = message.Source,
            DetailType = message.DetailType,
            Detail = message.Detail,
            Resources = message.Resources,
            TraceHeader = message.TraceHeader,
            Time = DateTime.UtcNow
        };
    }

    private static PutEventsRequestEntry ConvertStringToEntry(string detail)
    {
        return new PutEventsRequestEntry
        {
            Detail = detail,
            Time = DateTime.UtcNow
        };
    }
}
