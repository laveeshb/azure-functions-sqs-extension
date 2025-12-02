
namespace Azure.Functions.Extensions.SQS.Sample.V3;

using System.Linq;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using Azure.Functions.Extensions.SQS;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

public class QueueMessageOutput
{
    [FunctionName("QueueSingleMessageOutput")]
    public static void QueueSingleMessageOutput(
        [HttpTrigger(AuthorizationLevel.Anonymous)] HttpRequest req,
        ILogger log,
        [SqsQueueOut(QueueUrl = "%SQS_OUTPUT_QUEUE_URL%")] out SqsQueueMessage outMessage)
    {
        var message = req.Query["message"].FirstOrDefault() ?? "Default message";
        outMessage = new SqsQueueMessage
        {
            Body = message,
            QueueUrl = string.Empty // Will be set from attribute
        };

        log.LogInformation("Sent single message to SQS: {Message}", message);
    }

    [FunctionName("QueueFullMessageOutput")]
    public static void QueueFullMessageOutput(
        [HttpTrigger(AuthorizationLevel.Anonymous)] HttpRequest req,
        ILogger log,
        [SqsQueueOut(QueueUrl = "%SQS_OUTPUT_QUEUE_URL%")] out SendMessageRequest outMessage)
    {
        var message = req.Query["message"].FirstOrDefault() ?? "Default message";
        outMessage = new SendMessageRequest
        {
            MessageBody = message,
            DelaySeconds = 2,
            /*MessageAttributes = ... any supported message property*/
        };

        log.LogInformation("Sent message with delay to SQS: {Message}", message);
    }

    [FunctionName("QueueMultiMessageOutput")]
    public static async Task QueueMultiMessageOutput(
        [HttpTrigger(AuthorizationLevel.Anonymous)] HttpRequest req,
        ILogger log,
        [SqsQueueOut(QueueUrl = "%SQS_OUTPUT_QUEUE_URL%")] IAsyncCollector<SqsQueueMessage> messageWriter)
    {
        var message = req.Query["message"].FirstOrDefault() ?? "Default message";
        var outMessages = new[] { 1, 2, 3 }.Select(index => new SqsQueueMessage
        {
            Body = $"Hello {message} n°{index}",
            QueueUrl = string.Empty // Will be set from attribute
        });

        await Task.WhenAll(outMessages.Select(msg => messageWriter.AddAsync(msg)));
        log.LogInformation("Sent {Count} messages to SQS with base: {Message}", 3, message);
    }
}
