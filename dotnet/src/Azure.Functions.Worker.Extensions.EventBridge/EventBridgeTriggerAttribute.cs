namespace Azure.Functions.Worker.Extensions.EventBridge;

using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

/// <summary>
/// Attribute used to mark a function that should be triggered by Amazon EventBridge events.
/// Events are received via an SQS queue that is subscribed to EventBridge rules.
/// Compatible with Azure Functions isolated worker model.
/// </summary>
[InputConverter(typeof(EventBridgeMessageConverter))]
[ConverterFallbackBehavior(ConverterFallbackBehavior.Default)]
public sealed class EventBridgeTriggerAttribute : TriggerBindingAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventBridgeTriggerAttribute"/> class.
    /// </summary>
    /// <param name="queueUrl">The URL of the SQS queue that receives EventBridge events.</param>
    public EventBridgeTriggerAttribute(string queueUrl)
    {
        QueueUrl = queueUrl ?? throw new ArgumentNullException(nameof(queueUrl));
    }

    /// <summary>
    /// Gets the URL of the SQS queue that receives EventBridge events.
    /// </summary>
    public string QueueUrl { get; }

    /// <summary>
    /// Gets or sets the AWS Access Key ID. If not specified, uses AWS credential chain.
    /// </summary>
    public string? AWSKeyId { get; set; }

    /// <summary>
    /// Gets or sets the AWS Secret Access Key. If not specified, uses AWS credential chain.
    /// </summary>
    public string? AWSAccessKey { get; set; }

    /// <summary>
    /// Gets or sets the AWS Region (e.g., "us-east-1"). If not specified, uses AWS credential chain.
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of messages to retrieve in a single batch (1-10). Default is 10.
    /// </summary>
    public int MaxNumberOfMessages { get; set; } = 10;

    /// <summary>
    /// Gets or sets the wait time in seconds for long polling (0-20). Default is 20.
    /// </summary>
    public int WaitTimeSeconds { get; set; } = 20;

    /// <summary>
    /// Gets or sets the visibility timeout in seconds. Default is 30.
    /// </summary>
    public int VisibilityTimeout { get; set; } = 30;

    /// <summary>
    /// Gets or sets an optional filter pattern to match specific event patterns.
    /// </summary>
    public string? EventPattern { get; set; }

    /// <summary>
    /// Gets or sets an optional event source filter (e.g., "aws.ec2", "custom.myapp").
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Gets or sets an optional detail type filter.
    /// </summary>
    public string? DetailType { get; set; }
}
