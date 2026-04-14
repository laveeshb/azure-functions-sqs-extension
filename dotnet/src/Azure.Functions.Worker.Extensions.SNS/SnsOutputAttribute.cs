namespace Azure.Functions.Worker.Extensions.SNS;

using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

/// <summary>
/// Attribute used to configure an Amazon SNS output binding.
/// Compatible with Azure Functions isolated worker model.
/// Use on return type properties or method to publish messages to SNS.
/// </summary>
public sealed class SnsOutputAttribute : OutputBindingAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SnsOutputAttribute"/> class.
    /// </summary>
    /// <param name="topicArn">The ARN of the SNS topic to publish to.</param>
    public SnsOutputAttribute(string topicArn)
    {
        TopicArn = topicArn ?? throw new ArgumentNullException(nameof(topicArn));
    }

    /// <summary>
    /// Gets the ARN of the SNS topic to publish to.
    /// </summary>
    public string TopicArn { get; }

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
    /// Gets or sets the default message subject.
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Gets or sets the message group ID for FIFO topics.
    /// </summary>
    public string? MessageGroupId { get; set; }
}
