
namespace Azure.WebJobs.Extensions.SQS;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;

internal static class SqsQueueDepthReader
{
    private const string VisibleMessages = "ApproximateNumberOfMessages";
    private const string InFlightMessages = "ApproximateNumberOfMessagesNotVisible";

    /// <summary>
    /// Returns ApproximateNumberOfMessages + ApproximateNumberOfMessagesNotVisible.
    /// </summary>
    public static async Task<long> GetQueueLengthAsync(IAmazonSQS client, string queueUrl)
    {
        var response = await client.GetQueueAttributesAsync(new GetQueueAttributesRequest
        {
            QueueUrl = queueUrl,
            AttributeNames = new List<string> { VisibleMessages, InFlightMessages }
        });

        // The SDK reads a missing attribute as 0, which would look like an empty queue and vote to scale in.
        if (response.Attributes is null
            || !response.Attributes.ContainsKey(VisibleMessages)
            || !response.Attributes.ContainsKey(InFlightMessages))
        {
            throw new InvalidOperationException($"GetQueueAttributes returned no message counts for {queueUrl}.");
        }

        return (long)response.ApproximateNumberOfMessages + response.ApproximateNumberOfMessagesNotVisible;
    }
}
