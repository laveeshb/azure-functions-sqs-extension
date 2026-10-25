
namespace Azure.WebJobs.Extensions.SQS;

using Microsoft.Azure.WebJobs.Host.Scale;

/// <summary>
/// Queue depth sample used by <see cref="SqsQueueScaleMonitor"/>.
/// </summary>
public class SqsQueueScaleMetrics : ScaleMetrics
{
    /// <summary>
    /// Visible plus in-flight messages. Both counts are approximate (eventually consistent).
    /// </summary>
    public long QueueLength { get; set; }
}
