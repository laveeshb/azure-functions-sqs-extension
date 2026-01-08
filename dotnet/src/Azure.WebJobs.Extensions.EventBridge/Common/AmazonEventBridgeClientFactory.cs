// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Azure.WebJobs.Extensions.EventBridge;

using System;
using Amazon;
using Amazon.EventBridge;
using Amazon.Runtime;

/// <summary>
/// Factory for creating Amazon EventBridge clients with proper configuration.
/// </summary>
internal static class AmazonEventBridgeClientFactory
{
    /// <summary>
    /// Builds an AmazonEventBridgeClient from the output attribute configuration.
    /// </summary>
    public static AmazonEventBridgeClient Build(EventBridgeOutAttribute attribute)
    {
        ArgumentNullException.ThrowIfNull(attribute);

        var config = new AmazonEventBridgeConfig();

        if (!string.IsNullOrEmpty(attribute.Region))
        {
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(attribute.Region);
        }

        if (!string.IsNullOrEmpty(attribute.AWSKeyId) && !string.IsNullOrEmpty(attribute.AWSAccessKey))
        {
            var credentials = new BasicAWSCredentials(attribute.AWSKeyId, attribute.AWSAccessKey);
            return new AmazonEventBridgeClient(credentials, config);
        }

        // Use default credential chain (environment variables, IAM role, etc.)
        return new AmazonEventBridgeClient(config);
    }
}
