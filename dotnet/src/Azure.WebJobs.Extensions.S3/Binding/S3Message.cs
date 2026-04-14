// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Azure.WebJobs.Extensions.S3;

/// <summary>
/// Represents an object to be uploaded to Amazon S3.
/// </summary>
public class S3Message
{
    /// <summary>
    /// Gets or sets the object key (path) within the bucket.
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// Gets or sets the content as a string.
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Gets or sets the content as bytes.
    /// </summary>
    public byte[]? ContentBytes { get; set; }

    /// <summary>
    /// Gets or sets the content as a stream.
    /// </summary>
    public Stream? ContentStream { get; set; }

    /// <summary>
    /// Gets or sets the bucket name. If not specified, uses the BucketName from the attribute.
    /// </summary>
    public string? BucketName { get; set; }

    /// <summary>
    /// Gets or sets the content type (MIME type) of the object.
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Gets or sets custom metadata for the object.
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the storage class for the object.
    /// </summary>
    public string? StorageClass { get; set; }

    /// <summary>
    /// Gets or sets the server-side encryption algorithm.
    /// </summary>
    public string? ServerSideEncryption { get; set; }
}
