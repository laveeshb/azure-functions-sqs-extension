
namespace Azure.WebJobs.Extensions.SQS;

using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Extensions.Options;

public class SqsQueueTriggerBindingProvider : ITriggerBindingProvider
{
    private readonly IOptions<SqsQueueOptions> _sqsQueueOptions;
    private readonly INameResolver _nameResolver;

    public SqsQueueTriggerBindingProvider(IOptions<SqsQueueOptions> sqsQueueOptions, INameResolver nameResolver)
    {
        _sqsQueueOptions = sqsQueueOptions ?? throw new ArgumentNullException(nameof(sqsQueueOptions));
        _nameResolver = nameResolver ?? throw new ArgumentNullException(nameof(nameResolver));
    }

    public Task<ITriggerBinding?> TryCreateAsync(TriggerBindingProviderContext context)
    {
        var triggerAttribute = context.Parameter.GetCustomAttribute<SqsQueueTriggerAttribute>(inherit: false);
        return triggerAttribute is null
            ? Task.FromResult<ITriggerBinding?>(null)
            : Task.FromResult<ITriggerBinding?>(new SqsQueueTriggerBinding(
                parameterInfo: context.Parameter, 
                triggerParameters: ResolveTriggerParameters(triggerAttribute), 
                sqsQueueOptions: _sqsQueueOptions));
    }

    private SqsQueueTriggerAttribute ResolveTriggerParameters(SqsQueueTriggerAttribute triggerAttribute)
    {
        return new SqsQueueTriggerAttribute
        {
            AWSKeyId = Resolve(triggerAttribute.AWSKeyId),
            AWSAccessKey = Resolve(triggerAttribute.AWSAccessKey),
            QueueUrl = Resolve(triggerAttribute.QueueUrl) ?? triggerAttribute.QueueUrl,
            Region = Resolve(triggerAttribute.Region),
            ServiceUrl = Resolve(triggerAttribute.ServiceUrl)
        };
    }

    private string? Resolve(string? property)
    {
        if (string.IsNullOrEmpty(property))
            return property;

        return _nameResolver.Resolve(property) ?? _nameResolver.ResolveWholeString(property) ?? property;
    }
}
