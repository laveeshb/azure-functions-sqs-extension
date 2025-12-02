
namespace Azure.WebJobs.Extensions.SQS;

/// <summary>
/// Represents a message to send to an SQS queue
/// </summary>
public class SqsQueueMessage
{
    /// <summary>
    /// The message body content
    /// </summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// The target queue URL
    /// </summary>
    public string QueueUrl { get; set; } = string.Empty;
}
