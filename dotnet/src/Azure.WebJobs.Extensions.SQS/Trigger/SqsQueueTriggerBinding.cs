
namespace Azure.WebJobs.Extensions.SQS;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Extensions.Options;

public class SqsQueueTriggerBinding : ITriggerBinding
{
    private readonly IOptions<SqsQueueOptions> _sqsQueueOptions;
    private readonly SqsQueueTriggerAttribute _triggerParameters;
    private readonly ParameterInfo _parameterInfo;

    public Type TriggerValueType => typeof(Message);

    public IReadOnlyDictionary<string, Type> BindingDataContract { get; } = new Dictionary<string, Type>();

    public SqsQueueTriggerBinding(
        ParameterInfo parameterInfo, 
        SqsQueueTriggerAttribute triggerParameters, 
        IOptions<SqsQueueOptions> sqsQueueOptions)
    {
        _parameterInfo = parameterInfo ?? throw new ArgumentNullException(nameof(parameterInfo));
        _triggerParameters = triggerParameters ?? throw new ArgumentNullException(nameof(triggerParameters));
        _sqsQueueOptions = sqsQueueOptions ?? throw new ArgumentNullException(nameof(sqsQueueOptions));
    }

    public Task<ITriggerData> BindAsync(object value, ValueBindingContext context)
    {
        return Task.FromResult<ITriggerData>(new TriggerData(
            valueProvider: new SqsQueueMessageValueProvider(value),
            bindingData: new Dictionary<string, object>()));
    }

    public Task<IListener> CreateListenerAsync(ListenerFactoryContext context)
    {
        return Task.FromResult<IListener>(new SqsQueueTriggerListener(
            triggerParameters: _triggerParameters,
            sqsQueueOptions: _sqsQueueOptions,
            executor: context.Executor));
    }

    public ParameterDescriptor ToParameterDescriptor()
    {
        return new ParameterDescriptor
        {
            Name = _parameterInfo.Name
        };
    }
}
