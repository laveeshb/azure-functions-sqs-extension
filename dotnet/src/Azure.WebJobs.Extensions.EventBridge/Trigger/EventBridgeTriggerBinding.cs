// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Azure.WebJobs.Extensions.EventBridge;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Extensions.Logging;

/// <summary>
/// Trigger binding for EventBridge HTTP webhook events via API Destination.
/// </summary>
public class EventBridgeTriggerBinding : ITriggerBinding
{
    private readonly EventBridgeTriggerAttribute _attribute;
    private readonly ParameterInfo _parameterInfo;
    private readonly ILoggerFactory _loggerFactory;
    private readonly EventBridgeWebhookHandler _webhookHandler;

    public Type TriggerValueType => typeof(EventBridgeEvent);

    public IReadOnlyDictionary<string, Type> BindingDataContract { get; } = new Dictionary<string, Type>
    {
        { "Id", typeof(string) },
        { "Source", typeof(string) },
        { "DetailType", typeof(string) },
        { "Account", typeof(string) },
        { "Region", typeof(string) },
        { "Time", typeof(DateTime) }
    };

    public EventBridgeTriggerBinding(
        ParameterInfo parameterInfo,
        EventBridgeTriggerAttribute attribute,
        ILoggerFactory loggerFactory,
        EventBridgeWebhookHandler webhookHandler)
    {
        _parameterInfo = parameterInfo ?? throw new ArgumentNullException(nameof(parameterInfo));
        _attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _webhookHandler = webhookHandler ?? throw new ArgumentNullException(nameof(webhookHandler));
    }

    public Task<ITriggerData> BindAsync(object value, ValueBindingContext context)
    {
        var eventBridgeEvent = value as EventBridgeEvent 
            ?? throw new ArgumentException("Expected EventBridgeEvent", nameof(value));

        var bindingData = new Dictionary<string, object?>
        {
            { "Id", eventBridgeEvent.Id },
            { "Source", eventBridgeEvent.Source },
            { "DetailType", eventBridgeEvent.DetailType },
            { "Account", eventBridgeEvent.Account },
            { "Region", eventBridgeEvent.Region },
            { "Time", eventBridgeEvent.Time }
        };

        // Use the parameter type to enable automatic deserialization to EventBridgeEvent<TDetail>, etc.
        var targetType = _parameterInfo.ParameterType;

        return Task.FromResult<ITriggerData>(new TriggerData(
            valueProvider: new EventBridgeEventValueProvider(eventBridgeEvent, targetType),
            bindingData: bindingData!));
    }

    public Task<IListener> CreateListenerAsync(ListenerFactoryContext context)
    {
        var listener = new EventBridgeTriggerListener(
            attribute: _attribute,
            executor: context.Executor,
            loggerFactory: _loggerFactory,
            webhookHandler: _webhookHandler,
            functionDescriptor: context.Descriptor);

        return Task.FromResult<IListener>(listener);
    }

    public ParameterDescriptor ToParameterDescriptor()
    {
        return new ParameterDescriptor
        {
            Name = _parameterInfo.Name,
            DisplayHints = new ParameterDisplayHints
            {
                Description = "EventBridge event via API Destination webhook"
            }
        };
    }
}
