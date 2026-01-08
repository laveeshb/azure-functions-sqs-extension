namespace Azure.Functions.Worker.Extensions.Kinesis;

using System.Text.Json.Serialization;

/// <summary>
/// Represents a Kinesis stream record.
/// </summary>
public class KinesisRecord
{
    /// <summary>
    /// The unique identifier of the record within its shard.
    /// </summary>
    [JsonPropertyName("sequenceNumber")]
    public string? SequenceNumber { get; set; }

    /// <summary>
    /// The approximate time that the record was inserted into the stream.
    /// </summary>
    [JsonPropertyName("approximateArrivalTimestamp")]
    public DateTime ApproximateArrivalTimestamp { get; set; }

    /// <summary>
    /// Identifies which shard in the stream the data record is assigned to.
    /// </summary>
    [JsonPropertyName("partitionKey")]
    public string? PartitionKey { get; set; }

    /// <summary>
    /// The data blob as a base64-encoded string.
    /// </summary>
    [JsonPropertyName("data")]
    public string? Data { get; set; }

    /// <summary>
    /// The encryption type used on the record.
    /// </summary>
    [JsonPropertyName("encryptionType")]
    public string? EncryptionType { get; set; }

    /// <summary>
    /// The shard ID of the shard from which the record was retrieved.
    /// </summary>
    [JsonPropertyName("shardId")]
    public string? ShardId { get; set; }

    /// <summary>
    /// Gets the data as a decoded string (assumes UTF-8 encoding).
    /// </summary>
    [JsonIgnore]
    public string? DecodedData
    {
        get
        {
            if (string.IsNullOrEmpty(Data))
            {
                return null;
            }
            try
            {
                var bytes = Convert.FromBase64String(Data);
                return System.Text.Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return Data;
            }
        }
    }
}

/// <summary>
/// Represents a Kinesis stream record with a strongly-typed data payload.
/// </summary>
/// <typeparam name="TData">The type of the record data.</typeparam>
public class KinesisRecord<TData>
{
    /// <summary>
    /// The unique identifier of the record within its shard.
    /// </summary>
    [JsonPropertyName("sequenceNumber")]
    public string? SequenceNumber { get; set; }

    /// <summary>
    /// The approximate time that the record was inserted into the stream.
    /// </summary>
    [JsonPropertyName("approximateArrivalTimestamp")]
    public DateTime ApproximateArrivalTimestamp { get; set; }

    /// <summary>
    /// Identifies which shard in the stream the data record is assigned to.
    /// </summary>
    [JsonPropertyName("partitionKey")]
    public string? PartitionKey { get; set; }

    /// <summary>
    /// The strongly-typed data payload.
    /// </summary>
    [JsonPropertyName("data")]
    public TData? Data { get; set; }

    /// <summary>
    /// The encryption type used on the record.
    /// </summary>
    [JsonPropertyName("encryptionType")]
    public string? EncryptionType { get; set; }

    /// <summary>
    /// The shard ID.
    /// </summary>
    [JsonPropertyName("shardId")]
    public string? ShardId { get; set; }
}

/// <summary>
/// Represents a record to be written to a Kinesis stream.
/// </summary>
public class KinesisOutputRecord
{
    /// <summary>
    /// The data to write (string, byte[], or any serializable object).
    /// </summary>
    public object? Data { get; set; }

    /// <summary>
    /// The partition key for the record.
    /// </summary>
    public string? PartitionKey { get; set; }

    /// <summary>
    /// The explicit hash key for the record.
    /// </summary>
    public string? ExplicitHashKey { get; set; }

    /// <summary>
    /// The stream name. If not specified, uses the attribute's StreamName.
    /// </summary>
    public string? StreamName { get; set; }
}
