namespace Azure.Functions.Extensions.SQS.Sample.Functions;

using Amazon.SQS.Model;
using Azure.Functions.Extensions.SQS;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

public class SqsOutputFunction
{
    private readonly ILogger<SqsOutputFunction> _logger;

    public SqsOutputFunction(ILogger<SqsOutputFunction> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// HTTP triggered function that sends a simple message to SQS queue
    /// Example: curl "http://localhost:7071/api/send-simple?message=Hello"
    /// </summary>
    [Function(nameof(SendSimpleMessage))]
    public IActionResult SendSimpleMessage(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req,
        [SqsQueueOut(QueueUrl = "%SQS_OUTPUT_QUEUE_URL%")] out SqsQueueMessage outMessage)
    {
        var message = req.Query["message"].ToString() ?? "Default message";
        
        outMessage = new SqsQueueMessage
        {
            Body = message,
            QueueUrl = string.Empty // Will be set from attribute
        };

        _logger.LogInformation("Sent simple message to SQS: {Message}", message);
        
        return new OkObjectResult(new { status = "Message sent", message });
    }

    /// <summary>
    /// Send a full SQS message with delay and attributes
    /// Example: curl "http://localhost:7071/api/send-delayed?message=Hello&delay=5"
    /// </summary>
    [Function(nameof(SendDelayedMessage))]
    public IActionResult SendDelayedMessage(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req,
        [SqsQueueOut(QueueUrl = "%SQS_OUTPUT_QUEUE_URL%")] out SendMessageRequest outMessage)
    {
        var message = req.Query["message"].ToString() ?? "Default delayed message";
        var delaySeconds = int.TryParse(req.Query["delay"], out var delay) ? delay : 2;

        outMessage = new SendMessageRequest
        {
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

        _logger.LogInformation("Sent delayed message ({Delay}s) to SQS: {Message}", delaySeconds, message);
        
        return new OkObjectResult(new 
        { 
            status = "Message sent with delay", 
            message, 
            delaySeconds 
        });
    }

    /// <summary>
    /// Send multiple messages using IAsyncCollector
    /// Example: curl "http://localhost:7071/api/send-batch?count=5&prefix=Test"
    /// </summary>
    [Function(nameof(SendBatchMessages))]
    public async Task<IActionResult> SendBatchMessages(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req,
        [SqsQueueOut(QueueUrl = "%SQS_OUTPUT_QUEUE_URL%")] IAsyncCollector<SqsQueueMessage> messageCollector)
    {
        var prefix = req.Query["prefix"].ToString() ?? "Message";
        var count = int.TryParse(req.Query["count"], out var c) ? c : 3;

        var messages = Enumerable.Range(1, count).Select(i => new SqsQueueMessage
        {
            Body = $"{prefix} #{i} - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}",
            QueueUrl = string.Empty
        });

        await Task.WhenAll(messages.Select(msg => messageCollector.AddAsync(msg)));

        _logger.LogInformation("Sent {Count} messages to SQS with prefix: {Prefix}", count, prefix);
        
        return new OkObjectResult(new 
        { 
            status = "Batch messages sent", 
            count,
            prefix
        });
    }

    /// <summary>
    /// Example with explicit AWS credentials (for backward compatibility)
    /// Note: Using credential chain (environment variables, IAM roles) is recommended
    /// </summary>
    [Function(nameof(SendWithExplicitCredentials))]
    public IActionResult SendWithExplicitCredentials(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req,
        [SqsQueueOut(
            QueueUrl = "%SQS_OUTPUT_QUEUE_URL%",
            AWSKeyId = "%AWS_ACCESS_KEY_ID%",
            AWSAccessKey = "%AWS_SECRET_ACCESS_KEY%",
            Region = "us-east-1")] out SqsQueueMessage outMessage)
    {
        var message = req.Query["message"].ToString() ?? "Message with explicit credentials";
        
        outMessage = new SqsQueueMessage
        {
            Body = message,
            QueueUrl = string.Empty
        };

        _logger.LogInformation("Sent message with explicit credentials: {Message}", message);
        
        return new OkObjectResult(new { status = "Message sent (explicit credentials)", message });
    }
}
