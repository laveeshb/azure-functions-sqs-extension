namespace Azure.Functions.Worker.Extensions.Kinesis;

using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

/// <summary>
/// Attribute used to mark a function that should be triggered by Amazon Kinesis stream records.
/// Compatible with Azure Functions isolated worker model.
/// </summary>
[InputConverter(typeof(KinesisRecordConverter))]
[ConverterFallbackBehavior(ConverterFallbackBehavior.Default)]
public sealed class KinesisTriggerAttribute : TriggerBindingAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KinesisTriggerAttribute"/> class.
    /// </summary>
    /// <param name="streamName">The name of the Kinesis stream.</param>
    public KinesisTriggerAttribute(string streamName)
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
    /// Gets or sets the starting position for reading records.
    /// Valid values: TRIM_HORIZON, LATEST, AT_TIMESTAMP. Default is TRIM_HORIZON.
    /// </summary>
    public string StartingPosition { get; set; } = "TRIM_HORIZON";

    /// <summary>
    /// Gets or sets the maximum number of records to retrieve per batch. Default is 100.
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the polling interval in milliseconds. Default is 1000.
    /// </summary>
    public int PollingIntervalMs { get; set; } = 1000;
}
