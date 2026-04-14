// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Azure.WebJobs.Extensions.EventBridge;

using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Extensions.Logging;

/// <summary>
/// Trigger binding provider for EventBridge HTTP webhook triggers via API Destination.
/// </summary>
public class EventBridgeTriggerBindingProvider : ITriggerBindingProvider
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly EventBridgeWebhookHandler _webhookHandler;

    public EventBridgeTriggerBindingProvider(
        ILoggerFactory loggerFactory,
        EventBridgeWebhookHandler webhookHandler)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _webhookHandler = webhookHandler ?? throw new ArgumentNullException(nameof(webhookHandler));
    }

    public Task<ITriggerBinding?> TryCreateAsync(TriggerBindingProviderContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var parameter = context.Parameter;
        var attribute = parameter.GetCustomAttribute<EventBridgeTriggerAttribute>();

        if (attribute == null)
        {
            return Task.FromResult<ITriggerBinding?>(null);
        }

        var binding = new EventBridgeTriggerBinding(
            parameterInfo: parameter,
            attribute: attribute,
            loggerFactory: _loggerFactory,
            webhookHandler: _webhookHandler);

        return Task.FromResult<ITriggerBinding?>(binding);
    }
}
