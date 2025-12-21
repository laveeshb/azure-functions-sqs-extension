namespace Azure.Functions.Worker.Extensions.SQS;

using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

/// <summary>
/// Attribute used to configure an Amazon SQS output binding.
/// Compatible with Azure Functions isolated worker model.
/// Use on return type properties or method to send messages to SQS.
/// </summary>
public sealed class SqsOutputAttribute : OutputBindingAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqsOutputAttribute"/> class.
    /// </summary>
    /// <param name="queueUrl">The URL of the SQS queue to send messages to.</param>
    public SqsOutputAttribute(string queueUrl)
    {
        QueueUrl = queueUrl ?? throw new ArgumentNullException(nameof(queueUrl));
    }

    /// <summary>
    /// Gets the URL of the SQS queue to send messages to.
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
}
