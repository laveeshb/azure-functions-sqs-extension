// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Azure.WebJobs.Extensions.S3;

using System;
using Microsoft.Azure.WebJobs.Description;

/// <summary>
/// Attribute used to configure an Amazon S3 output binding for in-process Azure Functions.
/// </summary>
[Binding]
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
public sealed class S3OutAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the name of the S3 bucket.
    /// </summary>
    [AutoResolve]
    public string? BucketName { get; set; }

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
    /// Gets or sets the default key prefix for uploaded objects.
    /// </summary>
    [AutoResolve]
    public string? KeyPrefix { get; set; }
}
