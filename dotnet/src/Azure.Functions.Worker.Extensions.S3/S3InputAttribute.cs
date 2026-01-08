// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Azure.Functions.Worker.Extensions.S3;

using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

/// <summary>
/// Attribute used to configure an Amazon S3 input binding.
/// Compatible with Azure Functions isolated worker model.
/// Use on parameters to read objects from S3.
/// </summary>
public sealed class S3InputAttribute : InputBindingAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="S3InputAttribute"/> class.
    /// </summary>
    /// <param name="bucketName">The name of the S3 bucket.</param>
    /// <param name="key">The object key to read.</param>
    public S3InputAttribute(string bucketName, string key)
    {
        BucketName = bucketName ?? throw new ArgumentNullException(nameof(bucketName));
        Key = key ?? throw new ArgumentNullException(nameof(key));
    }

    /// <summary>
    /// Gets the name of the S3 bucket.
    /// </summary>
    public string BucketName { get; }

    /// <summary>
    /// Gets the object key to read.
    /// </summary>
    public string Key { get; }

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
    /// Gets or sets the version ID of the object to retrieve.
    /// If not specified, the latest version is retrieved.
    /// </summary>
    public string? VersionId { get; set; }
}
