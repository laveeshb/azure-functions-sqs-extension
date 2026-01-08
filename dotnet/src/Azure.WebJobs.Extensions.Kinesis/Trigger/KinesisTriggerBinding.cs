// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Azure.WebJobs.Extensions.Kinesis;

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
/// Trigger binding for Kinesis stream records.
/// </summary>
public class KinesisTriggerBinding : ITriggerBinding
{
    private readonly KinesisTriggerAttribute _attribute;
    private readonly ParameterInfo _parameterInfo;
    private readonly ILoggerFactory _loggerFactory;

    public Type TriggerValueType => typeof(KinesisRecord);

    public IReadOnlyDictionary<string, Type> BindingDataContract { get; } = new Dictionary<string, Type>
    {
        { "SequenceNumber", typeof(string) },
        { "PartitionKey", typeof(string) },
        { "ShardId", typeof(string) },
        { "StreamName", typeof(string) },
        { "ApproximateArrivalTimestamp", typeof(DateTime) }
    };

    public KinesisTriggerBinding(
        ParameterInfo parameterInfo,
        KinesisTriggerAttribute attribute,
        ILoggerFactory loggerFactory)
    {
        _parameterInfo = parameterInfo ?? throw new ArgumentNullException(nameof(parameterInfo));
        _attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    public Task<ITriggerData> BindAsync(object value, ValueBindingContext context)
    {
        var record = value as KinesisRecord 
            ?? throw new ArgumentException("Expected KinesisRecord", nameof(value));

        var bindingData = new Dictionary<string, object?>
        {
            { "SequenceNumber", record.SequenceNumber },
            { "PartitionKey", record.PartitionKey },
            { "ShardId", record.ShardId },
            { "StreamName", record.StreamName },
            { "ApproximateArrivalTimestamp", record.ApproximateArrivalTimestamp }
        };

        // Use the parameter type to enable automatic deserialization
        var targetType = _parameterInfo.ParameterType;

        return Task.FromResult<ITriggerData>(new TriggerData(
            valueProvider: new KinesisRecordValueProvider(record, targetType),
            bindingData: bindingData!));
    }

    public Task<IListener> CreateListenerAsync(ListenerFactoryContext context)
    {
        var listener = new KinesisTriggerListener(
            attribute: _attribute,
            executor: context.Executor,
            loggerFactory: _loggerFactory);

        return Task.FromResult<IListener>(listener);
    }

    public ParameterDescriptor ToParameterDescriptor()
    {
        return new ParameterDescriptor
        {
            Name = _parameterInfo.Name,
            DisplayHints = new ParameterDisplayHints
            {
                Description = $"Kinesis stream record from {_attribute.StreamName}"
            }
        };
    }
}
