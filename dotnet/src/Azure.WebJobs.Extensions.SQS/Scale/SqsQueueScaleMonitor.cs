
namespace Azure.WebJobs.Extensions.SQS;

using System;
using System.Linq;
using System.Threading.Tasks;
using Amazon.SQS;
using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Extensions.Logging;

public class SqsQueueScaleMonitor : IScaleMonitor<SqsQueueScaleMetrics>
{
    // SQS queue counters are eventually consistent, so scale in only after
    // the queue has looked empty for this many consecutive samples.
    internal const int ScaleInSampleCount = 5;

    private readonly IAmazonSQS _client;
    private readonly string _queueUrl;
    private readonly int _targetMessagesPerInstance;
    private readonly ILogger? _logger;

    public SqsQueueScaleMonitor(
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

        Descriptor = new ScaleMonitorDescriptor($"{functionId}-sqsqueuetrigger", functionId);
    }

    public ScaleMonitorDescriptor Descriptor { get; }

    public async Task<SqsQueueScaleMetrics> GetMetricsAsync()
    {
        try
        {
            return new SqsQueueScaleMetrics
            {
                QueueLength = await SqsQueueDepthReader.GetQueueLengthAsync(_client, _queueUrl)
            };
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Unable to read queue attributes for scaling: {QueueUrl}", _queueUrl);
            throw;
        }
    }

    async Task<ScaleMetrics> IScaleMonitor.GetMetricsAsync() => await GetMetricsAsync();

    public ScaleStatus GetScaleStatus(ScaleStatusContext<SqsQueueScaleMetrics> context)
    {
        var metrics = context.Metrics?.ToArray() ?? Array.Empty<SqsQueueScaleMetrics>();
        if (metrics.Length == 0)
            return new ScaleStatus { Vote = ScaleVote.None };

        var latest = metrics[^1];

        if (latest.QueueLength > 0 && context.WorkerCount == 0)
            return Vote(ScaleVote.ScaleOut, "Messages waiting and no workers.");

        if (latest.QueueLength > (long)context.WorkerCount * _targetMessagesPerInstance)
            return Vote(ScaleVote.ScaleOut, $"Queue length {latest.QueueLength} exceeds {context.WorkerCount} x {_targetMessagesPerInstance}.");

        if (metrics.Length >= ScaleInSampleCount
            && metrics.TakeLast(ScaleInSampleCount).All(m => m.QueueLength == 0))
            return Vote(ScaleVote.ScaleIn, "Queue empty for the last samples.");

        return new ScaleStatus { Vote = ScaleVote.None };
    }

    ScaleStatus IScaleMonitor.GetScaleStatus(ScaleStatusContext context) =>
        GetScaleStatus(new ScaleStatusContext<SqsQueueScaleMetrics>
        {
            WorkerCount = context.WorkerCount,
            Metrics = context.Metrics?.Cast<SqsQueueScaleMetrics>().ToArray()
        });

    private ScaleStatus Vote(ScaleVote vote, string reason)
    {
        _logger?.LogDebug("SQS scale vote {Vote} for {QueueUrl}: {Reason}", vote, _queueUrl, reason);
        return new ScaleStatus { Vote = vote };
    }
}
