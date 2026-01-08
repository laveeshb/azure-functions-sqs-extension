// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Azure.WebJobs.Extensions.SNS;

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
/// Trigger binding for SNS HTTP webhook notifications.
/// </summary>
public class SnsTriggerBinding : ITriggerBinding
{
    private readonly SnsTriggerAttribute _attribute;
    private readonly ParameterInfo _parameterInfo;
    private readonly ILoggerFactory _loggerFactory;
    private readonly SnsSignatureValidator _signatureValidator;
    private readonly SnsWebhookHandler _webhookHandler;

    public Type TriggerValueType => typeof(SnsNotification);

    public IReadOnlyDictionary<string, Type> BindingDataContract { get; } = new Dictionary<string, Type>
    {
        { "MessageId", typeof(string) },
        { "TopicArn", typeof(string) },
        { "Subject", typeof(string) },
        { "Message", typeof(string) },
        { "Timestamp", typeof(string) },
        { "Type", typeof(string) }
    };

    public SnsTriggerBinding(
        ParameterInfo parameterInfo,
        SnsTriggerAttribute attribute,
        ILoggerFactory loggerFactory,
        SnsSignatureValidator signatureValidator,
        SnsWebhookHandler webhookHandler)
    {
        _parameterInfo = parameterInfo ?? throw new ArgumentNullException(nameof(parameterInfo));
        _attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _signatureValidator = signatureValidator ?? throw new ArgumentNullException(nameof(signatureValidator));
        _webhookHandler = webhookHandler ?? throw new ArgumentNullException(nameof(webhookHandler));
    }

    public Task<ITriggerData> BindAsync(object value, ValueBindingContext context)
    {
        var notification = value as SnsNotification 
            ?? throw new ArgumentException("Expected SnsNotification", nameof(value));

        var bindingData = new Dictionary<string, object?>
        {
            { "MessageId", notification.MessageId },
            { "TopicArn", notification.TopicArn },
            { "Subject", notification.Subject },
            { "Message", notification.Message },
            { "Timestamp", notification.Timestamp },
            { "Type", notification.Type }
        };

        // Use the parameter type to enable automatic deserialization to S3EventNotification, etc.
        var targetType = _parameterInfo.ParameterType;

        return Task.FromResult<ITriggerData>(new TriggerData(
            valueProvider: new SnsNotificationValueProvider(notification, targetType),
            bindingData: bindingData!));
    }

    public Task<IListener> CreateListenerAsync(ListenerFactoryContext context)
    {
        var listener = new SnsTriggerListener(
            attribute: _attribute,
            executor: context.Executor,
            loggerFactory: _loggerFactory,
            signatureValidator: _signatureValidator,
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
                Description = "SNS HTTP webhook trigger"
            }
        };
    }
}
