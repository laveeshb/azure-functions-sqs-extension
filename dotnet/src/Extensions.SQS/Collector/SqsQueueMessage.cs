
namespace Azure.Functions.Extensions.SQS;

/// <summary>
/// Represents a message to send to an SQS queue
/// </summary>
public class SqsQueueMessage
{
    /// <summary>
    /// The message body content
    /// </summary>
    public required string Body { get; set; }

    /// <summary>
    /// The target queue URL
    /// </summary>
    public required string QueueUrl { get; set; }
}
