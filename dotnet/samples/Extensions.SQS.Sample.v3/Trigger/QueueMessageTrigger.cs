
namespace Azure.Functions.Extensions.SQS.Sample.V3;

using Amazon.SQS.Model;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

public static class QueueMessageTrigger
{
    [FunctionName("QueueMessageTrigger")]
    public static void Run(
        [SqsQueueTrigger(
            QueueUrl = "%SQS_QUEUE_URL%")] Message message,
        ILogger log)
    {
        log.LogInformation(
            "SQS Trigger - Message received: {MessageId}, Body: {Body}, Attributes: {AttributeCount}",
            message.MessageId,
            message.Body,
            message.MessageAttributes.Count);

        // Process message here
        // If function succeeds, message will be automatically deleted from queue
        // If function fails, message will become visible again after visibility timeout
    }
}
