namespace Azure.Functions.Extensions.SQS.Test.InProcess;

using Amazon.SQS.Model;
using Azure.WebJobs.Extensions.SQS;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

/// <summary>
/// In-Process (WebJobs) model SQS trigger functions.
/// Uses SqsQueueTriggerAttribute for the in-process hosting model.
/// </summary>
public class SqsTriggerFunction
{
    /// <summary>
    /// Triggers when a message is received from the SQS queue.
    /// Uses AWS credential chain - no hardcoded credentials needed!
    /// Configure AWS credentials via environment variables or AWS CLI.
    /// </summary>
    [FunctionName(nameof(ProcessSqsMessage))]
    public void ProcessSqsMessage(
        [SqsQueueTrigger(QueueUrl = "%SQS_QUEUE_URL%")] Message message,
        ILogger log)
    {
        log.LogInformation("=== SQS Message Received (In-Process Model) ===");
        log.LogInformation("Message ID: {MessageId}", message.MessageId);
        log.LogInformation("Body: {Body}", message.Body);
        log.LogInformation("Receipt Handle: {ReceiptHandle}", message.ReceiptHandle);
        log.LogInformation("Attributes: {AttributeCount}", message.Attributes.Count);
        log.LogInformation("Message Attributes: {MessageAttributeCount}", message.MessageAttributes.Count);

        // Process your message here
        // If this function succeeds, the message will be automatically deleted from the queue
        // If it fails, the message will become visible again after the visibility timeout

        log.LogInformation("Message processed successfully");
    }

    /// <summary>
    /// Example with async processing
    /// </summary>
    [FunctionName(nameof(ProcessSqsMessageAsync))]
    public async Task ProcessSqsMessageAsync(
        [SqsQueueTrigger(QueueUrl = "%SQS_QUEUE_URL%")] Message message,
        ILogger log)
    {
        log.LogInformation("Processing message asynchronously (In-Process Model): {MessageId}", message.MessageId);

        // Simulate async work
        await Task.Delay(100);

        log.LogInformation("Async processing completed for: {MessageId}", message.MessageId);
    }

    /// <summary>
    /// Example with string body binding instead of full Message object
    /// </summary>
    [FunctionName(nameof(ProcessSqsMessageBody))]
    public void ProcessSqsMessageBody(
        [SqsQueueTrigger(QueueUrl = "%SQS_QUEUE_URL%")] string messageBody,
        ILogger log)
    {
        log.LogInformation("Received message body (In-Process Model): {Body}", messageBody);
    }
}
