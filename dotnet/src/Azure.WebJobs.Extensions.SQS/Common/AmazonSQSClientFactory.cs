
namespace Azure.WebJobs.Extensions.SQS;

using System;
using System.Linq;
using Amazon;
using Amazon.Runtime;
using Amazon.SQS;
using Azure.WebJobs.Extensions.SQS.Auth;
using Microsoft.Extensions.Logging;
using AzureCore = global::Azure.Core;
using AzureIdentity = global::Azure.Identity;

public class AmazonSQSClientFactory
{
    public static AmazonSQSClient Build(SqsQueueTriggerAttribute triggerParameters, ILogger? logger = null)
    {
        return Build(
            queueUrl: triggerParameters.QueueUrl,
            awsKeyId: triggerParameters.AWSKeyId,
            awsAccessKey: triggerParameters.AWSAccessKey,
            regionOverride: triggerParameters.Region,
            awsRoleArn: triggerParameters.AwsRoleArn,
            awsStsAudience: triggerParameters.AwsStsAudience,
            awsRoleSessionName: triggerParameters.AwsRoleSessionName,
            awsSessionDurationSeconds: triggerParameters.AwsSessionDurationSeconds,
            entraTenantId: triggerParameters.EntraTenantId,
            entraClientId: triggerParameters.EntraClientId,
            entraClientSecret: triggerParameters.EntraClientSecret,
            logger: logger);
    }

    public static AmazonSQSClient Build(SqsQueueOutAttribute outParameters, ILogger? logger = null)
    {
        return Build(
            queueUrl: outParameters.QueueUrl,
            awsKeyId: outParameters.AWSKeyId,
            awsAccessKey: outParameters.AWSAccessKey,
            regionOverride: outParameters.Region,
            awsRoleArn: outParameters.AwsRoleArn,
            awsStsAudience: outParameters.AwsStsAudience,
            awsRoleSessionName: outParameters.AwsRoleSessionName,
            awsSessionDurationSeconds: outParameters.AwsSessionDurationSeconds,
            entraTenantId: outParameters.EntraTenantId,
            entraClientId: outParameters.EntraClientId,
            entraClientSecret: outParameters.EntraClientSecret,
            logger: logger);
    }

    private static AmazonSQSClient Build(
        string queueUrl,
        string? awsKeyId,
        string? awsAccessKey,
        string? regionOverride,
        string? awsRoleArn,
        string awsStsAudience,
        string awsRoleSessionName,
        int awsSessionDurationSeconds,
        string? entraTenantId,
        string? entraClientId,
        string? entraClientSecret,
        ILogger? logger)
    {
        var sqsRegion = ExtractRegionFromQueueUrl(queueUrl);
        var region = !string.IsNullOrEmpty(regionOverride)
            ? RegionEndpoint.GetBySystemName(regionOverride)
            : RegionEndpoint.EnumerableAllRegions.Single(r => r.SystemName.Equals(sqsRegion, StringComparison.OrdinalIgnoreCase));

        var credentials = SelectCredentials(
            awsKeyId: awsKeyId,
            awsAccessKey: awsAccessKey,
            awsRoleArn: awsRoleArn,
            awsStsAudience: awsStsAudience,
            awsRoleSessionName: awsRoleSessionName,
            awsSessionDurationSeconds: awsSessionDurationSeconds,
            stsRegion: region,
            entraTenantId: entraTenantId,
            entraClientId: entraClientId,
            entraClientSecret: entraClientSecret,
            logger: logger);

        return credentials is null
            ? new AmazonSQSClient(region)
            : new AmazonSQSClient(credentials, region);
    }

    /// <summary>
    /// Selects the AWS credentials source. Returns <c>null</c> when the caller should
    /// fall back to the AWS SDK's default credential chain (env vars, shared file, etc.).
    /// Priority: Entra federation (when <paramref name="awsRoleArn"/> is set) → explicit
    /// access key + secret → default chain.
    /// </summary>
    internal static AWSCredentials? SelectCredentials(
        string? awsKeyId,
        string? awsAccessKey,
        string? awsRoleArn,
        string awsStsAudience,
        string awsRoleSessionName,
        int awsSessionDurationSeconds,
        RegionEndpoint stsRegion,
        string? entraTenantId,
        string? entraClientId,
        string? entraClientSecret,
        ILogger? logger = null)
    {
        // Entra ID federation takes precedence when a role ARN is provided.
        // No long-lived AWS secret is stored — an Entra token is exchanged for
        // temporary AWS credentials via STS AssumeRoleWithWebIdentity.
        if (!string.IsNullOrEmpty(awsRoleArn))
        {
            var entraCredential = BuildEntraCredential(entraTenantId, entraClientId, entraClientSecret);
            return new EntraIdFederatedCredentials(
                roleArn: awsRoleArn!,
                audience: awsStsAudience,
                roleSessionName: awsRoleSessionName,
                sessionDurationSeconds: awsSessionDurationSeconds,
                stsRegion: stsRegion,
                logger: logger,
                getEntraToken: entraCredential.GetTokenAsync);
        }

        if (string.IsNullOrEmpty(awsKeyId) || string.IsNullOrEmpty(awsAccessKey))
        {
            return null;
        }

        return new BasicAWSCredentials(accessKey: awsKeyId, secretKey: awsAccessKey);
    }

    /// <summary>
    /// Selects the Entra credential used to obtain the federation token. All three
    /// app-registration fields must be provided together or all omitted; partial
    /// configuration throws rather than silently falling back to DefaultAzureCredential,
    /// since that silent downgrade has bitten users who typo'd a Key Vault reference
    /// ("works in dev via az login, fails in prod"). When all three are omitted, uses
    /// DefaultAzureCredential — which picks up the Function App's managed identity
    /// in production and the developer's Entra identity locally.
    /// </summary>
    internal static AzureCore.TokenCredential BuildEntraCredential(
        string? entraTenantId,
        string? entraClientId,
        string? entraClientSecret)
    {
        var hasTenantId = !string.IsNullOrEmpty(entraTenantId);
        var hasClientId = !string.IsNullOrEmpty(entraClientId);
        var hasClientSecret = !string.IsNullOrEmpty(entraClientSecret);

        if (hasTenantId && hasClientId && hasClientSecret)
        {
            return new AzureIdentity.ClientSecretCredential(entraTenantId, entraClientId, entraClientSecret);
        }

        if (hasTenantId || hasClientId || hasClientSecret)
        {
            var missing = string.Join(", ",
                new[]
                {
                    hasTenantId ? null : nameof(SqsQueueTriggerAttribute.EntraTenantId),
                    hasClientId ? null : nameof(SqsQueueTriggerAttribute.EntraClientId),
                    hasClientSecret ? null : nameof(SqsQueueTriggerAttribute.EntraClientSecret),
                }.Where(x => x is not null));
            throw new InvalidOperationException(
                $"Partial Entra app-registration configuration: missing {missing}. " +
                "Provide all three (EntraTenantId, EntraClientId, EntraClientSecret) or omit all three to use managed identity.");
        }

        return new AzureIdentity.DefaultAzureCredential();
    }

    private static string ExtractRegionFromQueueUrl(string queueUrl)
    {
        // URL format: https://sqs.{region}.amazonaws.com/{account-id}/{queue-name}
        var uri = new Uri(queueUrl);
        var hostParts = uri.Host.Split('.');

        if (hostParts.Length >= 3 && hostParts[0] == "sqs")
        {
            return hostParts[1];
        }

        throw new ArgumentException($"Unable to extract AWS region from queue URL: {queueUrl}");
    }
}
