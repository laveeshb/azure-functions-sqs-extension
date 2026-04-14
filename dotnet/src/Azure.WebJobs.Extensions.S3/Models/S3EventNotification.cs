// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Azure.WebJobs.Extensions.S3;

using System.Collections.Generic;
using System.Text.Json.Serialization;

/// <summary>
/// Represents an Amazon S3 event notification.
/// This is the root object received from S3 via SQS or SNS.
/// </summary>
public class S3EventNotification
{
    /// <summary>
    /// Gets or sets the list of S3 event records.
    /// </summary>
    [JsonPropertyName("Records")]
    public List<S3EventNotificationRecord>? Records { get; set; }
}

/// <summary>
/// Represents a single S3 event record within a notification.
/// </summary>
public class S3EventNotificationRecord
{
    /// <summary>
    /// Gets or sets the event version (e.g., "2.1").
    /// </summary>
    [JsonPropertyName("eventVersion")]
    public string? EventVersion { get; set; }

    /// <summary>
    /// Gets or sets the event source (always "aws:s3").
    /// </summary>
    [JsonPropertyName("eventSource")]
    public string? EventSource { get; set; }

    /// <summary>
    /// Gets or sets the AWS region where the event occurred.
    /// </summary>
    [JsonPropertyName("awsRegion")]
    public string? AwsRegion { get; set; }

    /// <summary>
    /// Gets or sets the time when S3 finished processing the request.
    /// </summary>
    [JsonPropertyName("eventTime")]
    public string? EventTime { get; set; }

    /// <summary>
    /// Gets or sets the event name/type (e.g., "ObjectCreated:Put").
    /// </summary>
    [JsonPropertyName("eventName")]
    public string? EventName { get; set; }

    /// <summary>
    /// Gets or sets information about the user who caused the event.
    /// </summary>
    [JsonPropertyName("userIdentity")]
    public S3UserIdentity? UserIdentity { get; set; }

    /// <summary>
    /// Gets or sets information about the request that caused the event.
    /// </summary>
    [JsonPropertyName("requestParameters")]
    public S3RequestParameters? RequestParameters { get; set; }

    /// <summary>
    /// Gets or sets the response elements from S3.
    /// </summary>
    [JsonPropertyName("responseElements")]
    public S3ResponseElements? ResponseElements { get; set; }

    /// <summary>
    /// Gets or sets the S3 bucket and object information.
    /// </summary>
    [JsonPropertyName("s3")]
    public S3Entity? S3 { get; set; }

    /// <summary>
    /// Gets or sets the Glacier event data (for restore events).
    /// </summary>
    [JsonPropertyName("glacierEventData")]
    public S3GlacierEventData? GlacierEventData { get; set; }

    /// <summary>
    /// Checks if this is an object creation event.
    /// </summary>
    public bool IsObjectCreated => EventName?.StartsWith("ObjectCreated:") == true;

    /// <summary>
    /// Checks if this is an object removal event.
    /// </summary>
    public bool IsObjectRemoved => EventName?.StartsWith("ObjectRemoved:") == true;

    /// <summary>
    /// Checks if this is a restore event (Glacier).
    /// </summary>
    public bool IsObjectRestore => EventName?.StartsWith("ObjectRestore:") == true;
}

/// <summary>
/// Contains information about the S3 bucket and object.
/// </summary>
public class S3Entity
{
    /// <summary>
    /// Gets or sets the schema version.
    /// </summary>
    [JsonPropertyName("s3SchemaVersion")]
    public string? S3SchemaVersion { get; set; }

    /// <summary>
    /// Gets or sets a unique identifier for the configuration that triggered this event.
    /// </summary>
    [JsonPropertyName("configurationId")]
    public string? ConfigurationId { get; set; }

    /// <summary>
    /// Gets or sets the bucket information.
    /// </summary>
    [JsonPropertyName("bucket")]
    public S3BucketEntity? Bucket { get; set; }

    /// <summary>
    /// Gets or sets the object information.
    /// </summary>
    [JsonPropertyName("object")]
    public S3ObjectEntity? Object { get; set; }
}

/// <summary>
/// Contains information about the S3 bucket.
/// </summary>
public class S3BucketEntity
{
    /// <summary>
    /// Gets or sets the bucket name.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the bucket owner's AWS account ID.
    /// </summary>
    [JsonPropertyName("ownerIdentity")]
    public S3UserIdentity? OwnerIdentity { get; set; }

    /// <summary>
    /// Gets or sets the Amazon Resource Name (ARN) of the bucket.
    /// </summary>
    [JsonPropertyName("arn")]
    public string? Arn { get; set; }
}

/// <summary>
/// Contains information about the S3 object.
/// </summary>
public class S3ObjectEntity
{
    /// <summary>
    /// Gets or sets the object key (path/name).
    /// </summary>
    [JsonPropertyName("key")]
    public string? Key { get; set; }

    /// <summary>
    /// Gets or sets the size of the object in bytes.
    /// </summary>
    [JsonPropertyName("size")]
    public long Size { get; set; }

    /// <summary>
    /// Gets or sets the object's ETag (entity tag).
    /// </summary>
    [JsonPropertyName("eTag")]
    public string? ETag { get; set; }

    /// <summary>
    /// Gets or sets the version ID of the object (for versioned buckets).
    /// </summary>
    [JsonPropertyName("versionId")]
    public string? VersionId { get; set; }

    /// <summary>
    /// Gets or sets the sequencer value for ordering events.
    /// </summary>
    [JsonPropertyName("sequencer")]
    public string? Sequencer { get; set; }
}

/// <summary>
/// Contains user identity information.
/// </summary>
public class S3UserIdentity
{
    /// <summary>
    /// Gets or sets the principal ID (AWS account ID or IAM user ID).
    /// </summary>
    [JsonPropertyName("principalId")]
    public string? PrincipalId { get; set; }
}

/// <summary>
/// Contains request parameter information.
/// </summary>
public class S3RequestParameters
{
    /// <summary>
    /// Gets or sets the source IP address of the request.
    /// </summary>
    [JsonPropertyName("sourceIPAddress")]
    public string? SourceIPAddress { get; set; }
}

/// <summary>
/// Contains response element information.
/// </summary>
public class S3ResponseElements
{
    /// <summary>
    /// Gets or sets the x-amz-request-id header value.
    /// </summary>
    [JsonPropertyName("x-amz-request-id")]
    public string? RequestId { get; set; }

    /// <summary>
    /// Gets or sets the x-amz-id-2 header value.
    /// </summary>
    [JsonPropertyName("x-amz-id-2")]
    public string? HostId { get; set; }
}

/// <summary>
/// Contains Glacier-specific event data.
/// </summary>
public class S3GlacierEventData
{
    /// <summary>
    /// Gets or sets the restore event data.
    /// </summary>
    [JsonPropertyName("restoreEventData")]
    public S3RestoreEventData? RestoreEventData { get; set; }
}

/// <summary>
/// Contains restore event data for Glacier objects.
/// </summary>
public class S3RestoreEventData
{
    /// <summary>
    /// Gets or sets the lifecycle restoration expiry time.
    /// </summary>
    [JsonPropertyName("lifecycleRestorationExpiryTime")]
    public string? LifecycleRestorationExpiryTime { get; set; }

    /// <summary>
    /// Gets or sets the lifecycle restore storage class.
    /// </summary>
    [JsonPropertyName("lifecycleRestoreStorageClass")]
    public string? LifecycleRestoreStorageClass { get; set; }
}
