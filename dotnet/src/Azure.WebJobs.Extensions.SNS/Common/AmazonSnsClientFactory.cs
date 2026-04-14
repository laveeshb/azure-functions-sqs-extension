// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Azure.WebJobs.Extensions.SNS;

using System;
using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.Runtime;

/// <summary>
/// Factory for creating Amazon SNS clients with proper configuration.
/// </summary>
internal static class AmazonSnsClientFactory
{
    /// <summary>
    /// Builds an AmazonSimpleNotificationServiceClient from the output attribute configuration.
    /// </summary>
    public static AmazonSimpleNotificationServiceClient Build(SnsOutAttribute attribute)
    {
        ArgumentNullException.ThrowIfNull(attribute);

        var config = new AmazonSimpleNotificationServiceConfig();

        if (!string.IsNullOrEmpty(attribute.Region))
        {
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(attribute.Region);
        }

        if (!string.IsNullOrEmpty(attribute.AWSKeyId) && !string.IsNullOrEmpty(attribute.AWSAccessKey))
        {
            var credentials = new BasicAWSCredentials(attribute.AWSKeyId, attribute.AWSAccessKey);
            return new AmazonSimpleNotificationServiceClient(credentials, config);
        }

        // Use default credential chain
        return new AmazonSimpleNotificationServiceClient(config);
    }
}
