namespace Azure.Functions.Worker.Extensions.S3;

using System.Text.Json.Serialization;

/// <summary>
/// Represents an S3 event notification.
/// </summary>
public class S3Event
{
    /// <summary>
    /// The list of S3 event records.
    /// </summary>
    [JsonPropertyName("Records")]
    public List<S3EventRecord>? Records { get; set; }
}

/// <summary>
/// Represents a single S3 event record.
/// </summary>
public class S3EventRecord
{
    /// <summary>
    /// The event version.
    /// </summary>
    [JsonPropertyName("eventVersion")]
    public string? EventVersion { get; set; }

    /// <summary>
    /// The event source (e.g., "aws:s3").
    /// </summary>
    [JsonPropertyName("eventSource")]
    public string? EventSource { get; set; }

    /// <summary>
    /// The AWS region.
    /// </summary>
    [JsonPropertyName("awsRegion")]
    public string? AwsRegion { get; set; }

    /// <summary>
    /// The event time.
    /// </summary>
    [JsonPropertyName("eventTime")]
    public DateTime EventTime { get; set; }

    /// <summary>
    /// The event name (e.g., "ObjectCreated:Put").
    /// </summary>
    [JsonPropertyName("eventName")]
    public string? EventName { get; set; }

    /// <summary>
    /// Information about the user that triggered the event.
    /// </summary>
    [JsonPropertyName("userIdentity")]
    public S3UserIdentity? UserIdentity { get; set; }

    /// <summary>
    /// Request parameters.
    /// </summary>
    [JsonPropertyName("requestParameters")]
    public S3RequestParameters? RequestParameters { get; set; }

    /// <summary>
    /// Response elements.
    /// </summary>
    [JsonPropertyName("responseElements")]
    public S3ResponseElements? ResponseElements { get; set; }

    /// <summary>
    /// The S3 details.
    /// </summary>
    [JsonPropertyName("s3")]
    public S3Details? S3 { get; set; }
}

/// <summary>
/// User identity information.
/// </summary>
public class S3UserIdentity
{
    /// <summary>
    /// The principal ID.
    /// </summary>
    [JsonPropertyName("principalId")]
    public string? PrincipalId { get; set; }
}

/// <summary>
/// Request parameters.
/// </summary>
public class S3RequestParameters
{
    /// <summary>
    /// Source IP address.
    /// </summary>
    [JsonPropertyName("sourceIPAddress")]
    public string? SourceIpAddress { get; set; }
}

/// <summary>
/// Response elements.
/// </summary>
public class S3ResponseElements
{
    /// <summary>
    /// Request ID.
    /// </summary>
    [JsonPropertyName("x-amz-request-id")]
    public string? RequestId { get; set; }

    /// <summary>
    /// Extended request ID.
    /// </summary>
    [JsonPropertyName("x-amz-id-2")]
    public string? Id2 { get; set; }
}

/// <summary>
/// S3-specific event details.
/// </summary>
public class S3Details
{
    /// <summary>
    /// S3 schema version.
    /// </summary>
    [JsonPropertyName("s3SchemaVersion")]
    public string? S3SchemaVersion { get; set; }

    /// <summary>
    /// Configuration ID.
    /// </summary>
    [JsonPropertyName("configurationId")]
    public string? ConfigurationId { get; set; }

    /// <summary>
    /// Bucket information.
    /// </summary>
    [JsonPropertyName("bucket")]
    public S3BucketInfo? Bucket { get; set; }

    /// <summary>
    /// Object information.
    /// </summary>
    [JsonPropertyName("object")]
    public S3ObjectInfo? Object { get; set; }
}

/// <summary>
/// S3 bucket information.
/// </summary>
public class S3BucketInfo
{
    /// <summary>
    /// Bucket name.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Owner identity.
    /// </summary>
    [JsonPropertyName("ownerIdentity")]
    public S3OwnerIdentity? OwnerIdentity { get; set; }

    /// <summary>
    /// Bucket ARN.
    /// </summary>
    [JsonPropertyName("arn")]
    public string? Arn { get; set; }
}

/// <summary>
/// S3 object information.
/// </summary>
public class S3ObjectInfo
{
    /// <summary>
    /// Object key.
    /// </summary>
    [JsonPropertyName("key")]
    public string? Key { get; set; }

    /// <summary>
    /// Object size in bytes.
    /// </summary>
    [JsonPropertyName("size")]
    public long Size { get; set; }

    /// <summary>
    /// Object ETag.
    /// </summary>
    [JsonPropertyName("eTag")]
    public string? ETag { get; set; }

    /// <summary>
    /// Object version ID.
    /// </summary>
    [JsonPropertyName("versionId")]
    public string? VersionId { get; set; }

    /// <summary>
    /// Sequencer.
    /// </summary>
    [JsonPropertyName("sequencer")]
    public string? Sequencer { get; set; }
}

/// <summary>
/// S3 owner identity.
/// </summary>
public class S3OwnerIdentity
{
    /// <summary>
    /// Principal ID.
    /// </summary>
    [JsonPropertyName("principalId")]
    public string? PrincipalId { get; set; }
}

/// <summary>
/// Represents an object to be written to S3.
/// </summary>
public class S3OutputObject
{
    /// <summary>
    /// The object key (path in the bucket).
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// The object content (string, byte[], or Stream).
    /// </summary>
    public object? Content { get; set; }

    /// <summary>
    /// The content type (MIME type).
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// The bucket name. If not specified, uses the attribute's BucketName.
    /// </summary>
    public string? BucketName { get; set; }

    /// <summary>
    /// Additional metadata for the object.
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }

    /// <summary>
    /// Tags for the object.
    /// </summary>
    public Dictionary<string, string>? Tags { get; set; }
}
