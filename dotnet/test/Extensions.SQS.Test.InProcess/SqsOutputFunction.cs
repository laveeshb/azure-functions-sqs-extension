namespace Azure.Functions.Extensions.SQS.Test.InProcess;

using Amazon.SQS.Model;
using Azure.WebJobs.Extensions.SQS;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

/// <summary>
/// In-Process (WebJobs) model HTTP triggered functions that send messages to SQS queue.
/// Uses SqsQueueOut attribute for output binding.
/// </summary>
public class SqsOutputFunction
{
    /// <summary>
    /// HTTP triggered function that sends a simple message to SQS queue using output binding
    /// Example: curl "http://localhost:7071/api/send-simple?message=Hello"
    /// </summary>
    [FunctionName(nameof(SendSimpleMessage))]
    public async Task<IActionResult> SendSimpleMessage(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "send-simple")] HttpRequest req,
        [SqsQueueOut(QueueUrl = "%SQS_OUTPUT_QUEUE_URL%")] IAsyncCollector<string> outputMessages,
        ILogger log)
    {
        var message = req.Query["message"].ToString();
        if (string.IsNullOrEmpty(message))
        {
            message = "Default message from in-process function";
        }

        await outputMessages.AddAsync(message);
        
        log.LogInformation("Sent simple message to SQS (In-Process Model): {Message}", message);
        
        return new OkObjectResult(new 
        { 
            status = "Message sent via output binding", 
            message,
            model = "in-process"
        });
    }

    /// <summary>
    /// Send multiple messages in one invocation
    /// Example: curl "http://localhost:7071/api/send-batch?count=5"
    /// </summary>
    [FunctionName(nameof(SendBatchMessages))]
    public async Task<IActionResult> SendBatchMessages(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "send-batch")] HttpRequest req,
        [SqsQueueOut(QueueUrl = "%SQS_OUTPUT_QUEUE_URL%")] IAsyncCollector<string> outputMessages,
        ILogger log)
    {
        var count = int.TryParse(req.Query["count"], out var c) ? c : 3;
        
        for (int i = 0; i < count; i++)
        {
            var message = $"Batch message {i + 1} of {count} at {DateTime.UtcNow:O}";
            await outputMessages.AddAsync(message);
        }

        log.LogInformation("Sent {Count} batch messages to SQS (In-Process Model)", count);
        
        return new OkObjectResult(new 
        { 
            status = "Batch messages sent", 
            count,
            model = "in-process"
        });
    }

    /// <summary>
    /// Send full Message objects with attributes
    /// Example: curl "http://localhost:7071/api/send-advanced?message=Hello"
    /// </summary>
    [FunctionName(nameof(SendAdvancedMessage))]
    public async Task<IActionResult> SendAdvancedMessage(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "send-advanced")] HttpRequest req,
        [SqsQueueOut(QueueUrl = "%SQS_OUTPUT_QUEUE_URL%")] IAsyncCollector<SendMessageRequest> outputMessages,
        ILogger log)
    {
        var message = req.Query["message"].ToString();
        if (string.IsNullOrEmpty(message))
        {
            message = "Advanced message from in-process function";
        }

        var sendRequest = new SendMessageRequest
        {
            MessageBody = message,
            DelaySeconds = 2,
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
                    StringValue = "AzureFunctions-InProcess"
                },
                ["Priority"] = new MessageAttributeValue
                {
                    DataType = "Number",
                    StringValue = "1"
                }
            }
        };

        await outputMessages.AddAsync(sendRequest);
        
        log.LogInformation("Sent advanced message to SQS (In-Process Model): {Message}", message);
        
        return new OkObjectResult(new 
        { 
            status = "Advanced message sent with attributes", 
            message,
            delaySeconds = 2,
            model = "in-process"
        });
    }
}
