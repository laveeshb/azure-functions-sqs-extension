
namespace Azure.WebJobs.Extensions.SQS;

using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;

internal static class SqsQueueDepthReader
{
    /// <summary>
    /// Returns ApproximateNumberOfMessages + ApproximateNumberOfMessagesNotVisible.
    /// </summary>
    public static async Task<long> GetQueueLengthAsync(IAmazonSQS client, string queueUrl)
    {
        var response = await client.GetQueueAttributesAsync(new GetQueueAttributesRequest
        {
            QueueUrl = queueUrl,
            AttributeNames = new List<string>
            {
                "ApproximateNumberOfMessages",
                "ApproximateNumberOfMessagesNotVisible"
            }
        });

        return (long)response.ApproximateNumberOfMessages + response.ApproximateNumberOfMessagesNotVisible;
    }
}
