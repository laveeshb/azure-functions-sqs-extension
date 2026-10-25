
namespace Azure.WebJobs.Extensions.SQS;

using System;
using System.Threading.Tasks;
using Amazon.SQS;
using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Extensions.Logging;

public class SqsQueueTargetScaler : ITargetScaler
{
    private readonly IAmazonSQS _client;
    private readonly string _queueUrl;
    private readonly int _targetMessagesPerInstance;
    private readonly ILogger? _logger;

    public SqsQueueTargetScaler(
        string functionId,
        IAmazonSQS client,
        string queueUrl,
        int targetMessagesPerInstance,
        ILogger? logger = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _queueUrl = queueUrl ?? throw new ArgumentNullException(nameof(queueUrl));
        _targetMessagesPerInstance = targetMessagesPerInstance > 0
            ? targetMessagesPerInstance
            : throw new ArgumentOutOfRangeException(nameof(targetMessagesPerInstance));
        _logger = logger;

        TargetScalerDescriptor = new TargetScalerDescriptor(functionId);
    }

    public TargetScalerDescriptor TargetScalerDescriptor { get; }

    public async Task<TargetScalerResult> GetScaleResultAsync(TargetScalerContext context)
    {
        long queueLength;
        try
        {
            queueLength = await SqsQueueDepthReader.GetQueueLengthAsync(_client, _queueUrl);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Unable to read queue attributes for scaling: {QueueUrl}", _queueUrl);
            throw;
        }

        var target = (int)Math.Min(int.MaxValue, (queueLength + _targetMessagesPerInstance - 1) / _targetMessagesPerInstance);

        _logger?.LogDebug(
            "SQS target scaler for {QueueUrl}: queue length {QueueLength}, target per instance {Target}, workers {Workers}",
            _queueUrl, queueLength, _targetMessagesPerInstance, target);

        return new TargetScalerResult { TargetWorkerCount = target };
    }
}
