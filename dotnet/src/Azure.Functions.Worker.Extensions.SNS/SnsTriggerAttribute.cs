namespace Azure.Functions.Worker.Extensions.SNS;

using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

/// <summary>
/// Attribute used to mark a function that should be triggered by Amazon SNS messages.
/// Messages are received via an SQS queue that is subscribed to an SNS topic.
/// Compatible with Azure Functions isolated worker model.
/// </summary>
[InputConverter(typeof(SnsMessageConverter))]
[ConverterFallbackBehavior(ConverterFallbackBehavior.Default)]
public sealed class SnsTriggerAttribute : TriggerBindingAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SnsTriggerAttribute"/> class.
    /// </summary>
    /// <param name="queueUrl">The URL of the SQS queue that receives SNS messages.</param>
    public SnsTriggerAttribute(string queueUrl)
    {
        QueueUrl = queueUrl ?? throw new ArgumentNullException(nameof(queueUrl));
    }

    /// <summary>
    /// Gets the URL of the SQS queue that receives SNS messages.
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
    /// Gets or sets an optional topic ARN filter to only process messages from a specific topic.
    /// </summary>
    public string? TopicArn { get; set; }

    /// <summary>
    /// Gets or sets an optional subject filter pattern.
    /// </summary>
    public string? SubjectFilter { get; set; }
}
