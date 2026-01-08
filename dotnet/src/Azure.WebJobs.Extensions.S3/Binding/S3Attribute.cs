// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Azure.WebJobs.Extensions.S3;

using System;
using Microsoft.Azure.WebJobs.Description;

/// <summary>
/// Attribute used to configure an Amazon S3 input binding for in-process Azure Functions.
/// Use on parameters to read objects from S3.
/// </summary>
[Binding]
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class S3Attribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="S3Attribute"/> class.
    /// </summary>
    public S3Attribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="S3Attribute"/> class.
    /// </summary>
    /// <param name="bucketName">The name of the S3 bucket.</param>
    /// <param name="key">The object key to read.</param>
    public S3Attribute(string bucketName, string key)
    {
        BucketName = bucketName;
        Key = key;
    }

    /// <summary>
    /// Gets or sets the name of the S3 bucket.
    /// </summary>
    [AutoResolve]
    public string? BucketName { get; set; }

    /// <summary>
    /// Gets or sets the object key to read.
    /// Supports binding expressions like {name} for route parameters.
    /// </summary>
    [AutoResolve]
    public string? Key { get; set; }

    /// <summary>
    /// Gets or sets the AWS Access Key ID. If not specified, uses AWS credential chain.
    /// </summary>
    [AutoResolve]
    public string? AWSKeyId { get; set; }

    /// <summary>
    /// Gets or sets the AWS Secret Access Key. If not specified, uses AWS credential chain.
    /// </summary>
    [AutoResolve]
    public string? AWSAccessKey { get; set; }

    /// <summary>
    /// Gets or sets the AWS Region (e.g., "us-east-1"). If not specified, uses AWS credential chain.
    /// </summary>
    [AutoResolve]
    public string? Region { get; set; }

    /// <summary>
    /// Gets or sets the version ID of the object to retrieve.
    /// If not specified, the latest version is retrieved.
    /// </summary>
    [AutoResolve]
    public string? VersionId { get; set; }
}
