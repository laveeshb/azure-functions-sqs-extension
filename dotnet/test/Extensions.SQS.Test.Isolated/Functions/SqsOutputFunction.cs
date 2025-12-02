namespace Azure.Functions.Extensions.SQS.Test.Isolated.Functions;

using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

/// <summary>
/// HTTP triggered functions that send messages to SQS queue using AWS SDK directly.
/// Note: The SQS output binding (SqsQueueOut) is designed for in-process Azure Functions model.
/// For isolated worker model, use the AWS SDK directly as shown in these examples.
/// </summary>
public class SqsOutputFunction
{
    private readonly ILogger<SqsOutputFunction> _logger;
    private readonly IAmazonSQS _sqsClient;
    private readonly string _outputQueueUrl;

    public SqsOutputFunction(ILogger<SqsOutputFunction> logger, IConfiguration configuration)
    {
        _logger = logger;
        _outputQueueUrl = configuration["SQS_OUTPUT_QUEUE_URL"] 
            ?? throw new InvalidOperationException("SQS_OUTPUT_QUEUE_URL not configured");
        
        // Use AWS credential chain (environment variables, IAM roles, credentials file)
        _sqsClient = new AmazonSQSClient();
    }

    /// <summary>
    /// HTTP triggered function that sends a simple message to SQS queue
    /// Example: curl "http://localhost:7071/api/send-simple?message=Hello"
    /// </summary>
    [Function(nameof(SendSimpleMessage))]
    public async Task<IActionResult> SendSimpleMessage(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
    {
        var message = req.Query["message"].ToString() ?? "Default message";
        
        var request = new SendMessageRequest
        {
            QueueUrl = _outputQueueUrl,
            MessageBody = message
        };

        var response = await _sqsClient.SendMessageAsync(request);
        
        _logger.LogInformation("Sent simple message to SQS: {Message}, MessageId: {MessageId}", 
            message, response.MessageId);
        
        return new OkObjectResult(new 
        { 
            status = "Message sent", 
            message,
            messageId = response.MessageId 
        });
    }

    /// <summary>
    /// Send a full SQS message with delay and attributes
    /// Example: curl "http://localhost:7071/api/send-delayed?message=Hello&delay=5"
    /// </summary>
    [Function(nameof(SendDelayedMessage))]
    public async Task<IActionResult> SendDelayedMessage(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
    {
        var message = req.Query["message"].ToString() ?? "Default delayed message";
        var delaySeconds = int.TryParse(req.Query["delay"], out var delay) ? delay : 2;

        var request = new SendMessageRequest
        {
            QueueUrl = _outputQueueUrl,
            MessageBody = message,
            DelaySeconds = delaySeconds,
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                ["Timestamp"] = new MessageAttributeValue
                {
                    DataType = "String",
                    StringValue = DateTime.UtcNow.ToString("O")
                },
                ["Source"] = new MessageAttributeValue
                {
                    DataType = "String",
                    StringValue = "AzureFunctions"
                }
            }
        };

        var response = await _sqsClient.SendMessageAsync(request);

        _logger.LogInformation("Sent delayed message ({Delay}s) to SQS: {Message}, MessageId: {MessageId}", 
            delaySeconds, message, response.MessageId);
        
        return new OkObjectResult(new 
        { 
            status = "Message sent with delay", 
            message, 
            delaySeconds,
            messageId = response.MessageId
        });
    }

    /// <summary>
    /// Send multiple messages in a batch
    /// Example: curl "http://localhost:7071/api/send-batch?count=5&prefix=Test"
    /// </summary>
    [Function(nameof(SendBatchMessages))]
    public async Task<IActionResult> SendBatchMessages(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
    {
        var prefix = req.Query["prefix"].ToString() ?? "Message";
        var count = int.TryParse(req.Query["count"], out var c) ? Math.Min(c, 10) : 3; // Max 10 per batch

        var entries = Enumerable.Range(1, count).Select(i => new SendMessageBatchRequestEntry
        {
            Id = i.ToString(),
            MessageBody = $"{prefix} #{i} - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}"
        }).ToList();

        var request = new SendMessageBatchRequest
        {
            QueueUrl = _outputQueueUrl,
            Entries = entries
        };

        var response = await _sqsClient.SendMessageBatchAsync(request);

        _logger.LogInformation("Sent {Count} messages to SQS with prefix: {Prefix}, Successful: {Successful}, Failed: {Failed}", 
            count, prefix, response.Successful.Count, response.Failed.Count);
        
        return new OkObjectResult(new 
        { 
            status = "Batch messages sent", 
            count,
            prefix,
            successful = response.Successful.Count,
            failed = response.Failed.Count,
            messageIds = response.Successful.Select(s => s.MessageId)
        });
    }
}
