
namespace Azure.Functions.Extensions.SQS;

using System;
using System.Linq;
using Amazon;
using Amazon.Runtime;
using Amazon.SQS;

public class AmazonSQSClientFactory
{
    public static AmazonSQSClient Build(SqsQueueTriggerAttribute triggerParameters)
    {
        return Build(
            queueUrl: triggerParameters.QueueUrl,
            awsKeyId: triggerParameters.AWSKeyId,
            awsAccessKey: triggerParameters.AWSAccessKey,
            regionOverride: triggerParameters.Region);
    }

    public static AmazonSQSClient Build(SqsQueueOutAttribute outParameters)
    {
        return Build(
            queueUrl: outParameters.QueueUrl,
            awsKeyId: outParameters.AWSKeyId,
            awsAccessKey: outParameters.AWSAccessKey,
            regionOverride: outParameters.Region);
    }

    private static AmazonSQSClient Build(string queueUrl, string? awsKeyId, string? awsAccessKey, string? regionOverride)
    {
        // Extract region from queue URL
        var sqsRegion = ExtractRegionFromQueueUrl(queueUrl);
        var region = !string.IsNullOrEmpty(regionOverride) 
            ? RegionEndpoint.GetBySystemName(regionOverride)
            : RegionEndpoint.EnumerableAllRegions.Single(r => r.SystemName.Equals(sqsRegion, StringComparison.OrdinalIgnoreCase));

        // Use AWS credential chain if no explicit credentials provided
        // This supports: Environment variables, ECS container credentials, EC2 instance profile, etc.
        if (string.IsNullOrEmpty(awsKeyId) || string.IsNullOrEmpty(awsAccessKey))
        {
            return new AmazonSQSClient(region);
        }

        // Fall back to explicit credentials if provided (for backward compatibility)
        var credentials = new BasicAWSCredentials(accessKey: awsKeyId, secretKey: awsAccessKey);
        return new AmazonSQSClient(credentials, region);
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
