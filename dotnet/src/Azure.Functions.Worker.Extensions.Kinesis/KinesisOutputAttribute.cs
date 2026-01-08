namespace Azure.Functions.Worker.Extensions.Kinesis;

using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

/// <summary>
/// Attribute used to configure an Amazon Kinesis output binding.
/// Compatible with Azure Functions isolated worker model.
/// Use on return type properties or method to write records to Kinesis.
/// </summary>
public sealed class KinesisOutputAttribute : OutputBindingAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KinesisOutputAttribute"/> class.
    /// </summary>
    /// <param name="streamName">The name of the Kinesis stream.</param>
    public KinesisOutputAttribute(string streamName)
    {
        StreamName = streamName ?? throw new ArgumentNullException(nameof(streamName));
    }

    /// <summary>
    /// Gets the name of the Kinesis stream.
    /// </summary>
    public string StreamName { get; }

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
    /// Gets or sets the default partition key for records.
    /// </summary>
    public string? PartitionKey { get; set; }
}
