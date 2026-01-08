// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Azure.WebJobs.Extensions.S3;

using System;
using Amazon;
using Amazon.S3;
using Amazon.Runtime;

/// <summary>
/// Factory for creating Amazon S3 clients with proper configuration.
/// </summary>
internal static class AmazonS3ClientFactory
{
    /// <summary>
    /// Builds an AmazonS3Client from the output attribute configuration.
    /// </summary>
    public static AmazonS3Client Build(S3OutAttribute attribute)
    {
        ArgumentNullException.ThrowIfNull(attribute);
        return Build(attribute.AWSKeyId, attribute.AWSAccessKey, attribute.Region);
    }

    /// <summary>
    /// Builds an AmazonS3Client from the input attribute configuration.
    /// </summary>
    public static AmazonS3Client Build(S3Attribute attribute)
    {
        ArgumentNullException.ThrowIfNull(attribute);
        return Build(attribute.AWSKeyId, attribute.AWSAccessKey, attribute.Region);
    }

    /// <summary>
    /// Builds an AmazonS3Client with the specified credentials and region.
    /// </summary>
    public static AmazonS3Client Build(string? awsKeyId, string? awsAccessKey, string? region)
    {
        var config = new AmazonS3Config();

        if (!string.IsNullOrEmpty(region))
        {
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(region);
        }

        if (!string.IsNullOrEmpty(awsKeyId) && !string.IsNullOrEmpty(awsAccessKey))
        {
            var credentials = new BasicAWSCredentials(awsKeyId, awsAccessKey);
            return new AmazonS3Client(credentials, config);
        }

        // Use default credential chain
        return new AmazonS3Client(config);
    }
}
