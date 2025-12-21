namespace Azure.Functions.Worker.Extensions.SQS;

using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

/// <summary>
/// Attribute used to mark a function that should be triggered by Amazon SQS queue messages.
/// Compatible with Azure Functions isolated worker model.
/// </summary>
[InputConverter(typeof(SqsMessageConverter))]
[ConverterFallbackBehavior(ConverterFallbackBehavior.Default)]
public sealed class SqsTriggerAttribute : TriggerBindingAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqsTriggerAttribute"/> class.
    /// </summary>
    /// <param name="queueUrl">The URL of the SQS queue to monitor.</param>
    public SqsTriggerAttribute(string queueUrl)
    {
        QueueUrl = queueUrl ?? throw new ArgumentNullException(nameof(queueUrl));
    }

    /// <summary>
    /// Gets the URL of the SQS queue to monitor.
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
    /// Gets or sets a custom SQS service URL for LocalStack or other SQS-compatible services.
    /// Example: "http://localhost:4566" for LocalStack.
    /// When specified, Region must also be provided.
    /// </summary>
    public string? ServiceUrl { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of messages to retrieve in a single batch (1-10). Default is 10.
    /// </summary>
    public int MaxNumberOfMessages { get; set; } = 10;

    /// <summary>
    /// Gets or sets the wait time in seconds for long polling (0-20). Default is 20.
    /// Longer wait times reduce API calls and costs.
    /// </summary>
    public int WaitTimeSeconds { get; set; } = 20;

    /// <summary>
    /// Gets or sets the visibility timeout in seconds. If not set, uses queue's default.
    /// </summary>
    public int? VisibilityTimeout { get; set; }

    /// <summary>
    /// Gets or sets whether to automatically delete messages after successful processing. Default is true.
    /// </summary>
    public bool AutoDelete { get; set; } = true;
}
