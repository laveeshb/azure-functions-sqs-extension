
namespace Azure.Functions.Extensions.SQS;

using System;

public class SqsQueueOptions
{
    /// <summary>
    /// Maximum number of messages to retrieve from SQS in a single request (1-10)
    /// </summary>
    public int? MaxNumberOfMessages { get; set; }

    /// <summary>
    /// Interval between polling requests to SQS
    /// </summary>
    public TimeSpan? PollingInterval { get; set; }

    /// <summary>
    /// Time that messages are hidden from other consumers after being retrieved
    /// </summary>
    public TimeSpan? VisibilityTimeout { get; set; }
}
