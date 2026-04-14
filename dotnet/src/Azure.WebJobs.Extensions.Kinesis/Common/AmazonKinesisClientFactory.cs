// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Azure.WebJobs.Extensions.Kinesis;

using System;
using Amazon;
using Amazon.Kinesis;
using Amazon.Runtime;

/// <summary>
/// Factory for creating Amazon Kinesis clients with proper configuration.
/// </summary>
internal static class AmazonKinesisClientFactory
{
    /// <summary>
    /// Builds an AmazonKinesisClient from the trigger attribute configuration.
    /// </summary>
    public static AmazonKinesisClient Build(KinesisTriggerAttribute attribute)
    {
        ArgumentNullException.ThrowIfNull(attribute);

        var config = new AmazonKinesisConfig();

        if (!string.IsNullOrEmpty(attribute.Region))
        {
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(attribute.Region);
        }

        if (!string.IsNullOrEmpty(attribute.AWSKeyId) && !string.IsNullOrEmpty(attribute.AWSAccessKey))
        {
            var credentials = new BasicAWSCredentials(attribute.AWSKeyId, attribute.AWSAccessKey);
            return new AmazonKinesisClient(credentials, config);
        }

        // Use default credential chain
        return new AmazonKinesisClient(config);
    }

    /// <summary>
    /// Builds an AmazonKinesisClient from the output attribute configuration.
    /// </summary>
    public static AmazonKinesisClient Build(KinesisOutAttribute attribute)
    {
        ArgumentNullException.ThrowIfNull(attribute);

        var config = new AmazonKinesisConfig();

        if (!string.IsNullOrEmpty(attribute.Region))
        {
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(attribute.Region);
        }

        if (!string.IsNullOrEmpty(attribute.AWSKeyId) && !string.IsNullOrEmpty(attribute.AWSAccessKey))
        {
            var credentials = new BasicAWSCredentials(attribute.AWSKeyId, attribute.AWSAccessKey);
            return new AmazonKinesisClient(credentials, config);
        }

        // Use default credential chain
        return new AmazonKinesisClient(config);
    }
}
