// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Azure.WebJobs.Extensions.S3;

using System;
using System.Collections.Generic;

/// <summary>
/// Represents an S3 object with its content and metadata.
/// Used as a target type for S3 input bindings.
/// </summary>
public sealed class S3Object
{
    /// <summary>
    /// Gets or sets the name of the bucket containing the object.
    /// </summary>
    public string? BucketName { get; set; }

    /// <summary>
    /// Gets or sets the object key.
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// Gets or sets the object content as a byte array.
    /// </summary>
    public byte[]? Content { get; set; }

    /// <summary>
    /// Gets or sets the content type (MIME type) of the object.
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Gets or sets the size of the object in bytes.
    /// </summary>
    public long ContentLength { get; set; }

    /// <summary>
    /// Gets or sets the entity tag (ETag) of the object.
    /// </summary>
    public string? ETag { get; set; }

    /// <summary>
    /// Gets or sets the last modified date of the object.
    /// </summary>
    public DateTime? LastModified { get; set; }

    /// <summary>
    /// Gets or sets the version ID of the object.
    /// </summary>
    public string? VersionId { get; set; }

    /// <summary>
    /// Gets or sets the user-defined metadata for the object.
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }

    /// <summary>
    /// Gets the content as a UTF-8 string.
    /// </summary>
    public string? GetContentAsString()
    {
        return Content != null 
            ? System.Text.Encoding.UTF8.GetString(Content) 
            : null;
    }
}
