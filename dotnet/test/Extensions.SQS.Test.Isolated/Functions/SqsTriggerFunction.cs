namespace Azure.Functions.Extensions.SQS.Test.Isolated.Functions;

using Amazon.SQS.Model;
using Azure.Functions.Worker.Extensions.SQS;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

public class SqsTriggerFunction
{
    private readonly ILogger<SqsTriggerFunction> _logger;

    public SqsTriggerFunction(ILogger<SqsTriggerFunction> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Triggers when a message is received from the SQS queue.
    /// Uses AWS credential chain - no hardcoded credentials needed!
    /// Configure AWS credentials via environment variables or AWS CLI.
    /// </summary>
    [Function(nameof(ProcessSqsMessage))]
    public void ProcessSqsMessage(
        [SqsTrigger("%SQS_QUEUE_URL%")] Message message)
    {
        _logger.LogInformation("=== SQS Message Received ===");
        _logger.LogInformation("Message ID: {MessageId}", message.MessageId);
        _logger.LogInformation("Body: {Body}", message.Body);
        _logger.LogInformation("Receipt Handle: {ReceiptHandle}", message.ReceiptHandle);
        _logger.LogInformation("Attributes: {AttributeCount}", message.Attributes.Count);
        _logger.LogInformation("Message Attributes: {MessageAttributeCount}", message.MessageAttributes.Count);

        // Process your message here
        // If this function succeeds, the message will be automatically deleted from the queue
        // If it fails, the message will become visible again after the visibility timeout

        _logger.LogInformation("Message processed successfully");
    }

    /// <summary>
    /// Example with async processing
    /// </summary>
    [Function(nameof(ProcessSqsMessageAsync))]
    public async Task ProcessSqsMessageAsync(
        [SqsTrigger("%SQS_QUEUE_URL%")] Message message)
    {
        _logger.LogInformation("Processing message asynchronously: {MessageId}", message.MessageId);

        // Simulate async work
        await Task.Delay(100);

        _logger.LogInformation("Async processing completed for: {MessageId}", message.MessageId);
    }
}
