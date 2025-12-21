
namespace Azure.WebJobs.Extensions.SQS;

using System;

public class SqsQueueOptions
{
    /// <summary>
    /// Maximum number of messages to retrieve from SQS in a single request (1-10)
    /// </summary>
    public int? MaxNumberOfMessages { get; set; }

    /// <summary>
    /// Delay between polling requests when the queue is empty.
    /// Note: SQS long polling (20s) already waits for messages, so this is an additional delay.
    /// Set to zero or null for immediate re-poll after long poll completes.
    /// </summary>
    public TimeSpan? PollingInterval { get; set; }

    /// <summary>
    /// Time that messages are hidden from other consumers after being retrieved
    /// </summary>
    public TimeSpan? VisibilityTimeout { get; set; }
}
