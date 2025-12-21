
namespace Azure.WebJobs.Extensions.SQS;

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
            regionOverride: triggerParameters.Region,
            serviceUrl: triggerParameters.ServiceUrl);
    }

    public static AmazonSQSClient Build(SqsQueueOutAttribute outParameters)
    {
        return Build(
            queueUrl: outParameters.QueueUrl,
            awsKeyId: outParameters.AWSKeyId,
            awsAccessKey: outParameters.AWSAccessKey,
            regionOverride: outParameters.Region,
            serviceUrl: outParameters.ServiceUrl);
    }

    private static AmazonSQSClient Build(string queueUrl, string? awsKeyId, string? awsAccessKey, string? regionOverride, string? serviceUrl)
    {
        // If a custom service URL is provided (e.g., LocalStack), use it directly
        if (!string.IsNullOrEmpty(serviceUrl))
        {
            return BuildWithServiceUrl(serviceUrl, regionOverride, awsKeyId, awsAccessKey);
        }

        // Extract region from queue URL or use override
        var region = GetRegion(queueUrl, regionOverride);

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

    private static AmazonSQSClient BuildWithServiceUrl(string serviceUrl, string? regionOverride, string? awsKeyId, string? awsAccessKey)
    {
        // Region is required when using custom service URL
        if (string.IsNullOrEmpty(regionOverride))
        {
            throw new ArgumentException(
                "Region must be specified when using a custom ServiceUrl (e.g., for LocalStack).");
        }

        var config = new AmazonSQSConfig
        {
            ServiceURL = serviceUrl,
            AuthenticationRegion = regionOverride
        };

        // Use AWS credential chain if no explicit credentials provided
        if (string.IsNullOrEmpty(awsKeyId) || string.IsNullOrEmpty(awsAccessKey))
        {
            return new AmazonSQSClient(config);
        }

        var credentials = new BasicAWSCredentials(accessKey: awsKeyId, secretKey: awsAccessKey);
        return new AmazonSQSClient(credentials, config);
    }

    private static RegionEndpoint GetRegion(string queueUrl, string? regionOverride)
    {
        if (!string.IsNullOrEmpty(regionOverride))
        {
            return RegionEndpoint.GetBySystemName(regionOverride);
        }

        var extractedRegion = ExtractRegionFromQueueUrl(queueUrl);
        if (extractedRegion != null)
        {
            return RegionEndpoint.GetBySystemName(extractedRegion);
        }

        throw new ArgumentException(
            $"Unable to extract AWS region from queue URL: {queueUrl}. " +
            "Please specify the Region parameter explicitly.");
    }

    private static string? ExtractRegionFromQueueUrl(string queueUrl)
    {
        // URL format: https://sqs.{region}.amazonaws.com/{account-id}/{queue-name}
        try
        {
            var uri = new Uri(queueUrl);
            var hostParts = uri.Host.Split('.');
            
            if (hostParts.Length >= 3 && hostParts[0] == "sqs")
            {
                return hostParts[1];
            }
        }
        catch
        {
            // Failed to parse URL, return null
        }

        return null;
    }
}
