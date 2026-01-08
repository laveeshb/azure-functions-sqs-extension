namespace Azure.Functions.Extensions.SQS.Test.InProcess;

using Azure.WebJobs.Extensions.SNS;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

/// <summary>
/// Sample functions demonstrating SNS trigger and output bindings.
/// </summary>
public class SnsFunctions
{
    #region Trigger Functions

    /// <summary>
    /// SNS Webhook Trigger - receives notifications from SNS HTTPS subscription.
    /// 
    /// Setup:
    /// 1. Deploy this function to Azure
    /// 2. Create an SNS HTTPS subscription pointing to: https://your-function.azurewebsites.net/api/webhooks/sns
    /// 3. The trigger automatically handles subscription confirmation
    /// </summary>
    [FunctionName(nameof(ProcessSnsNotification))]
    public async Task ProcessSnsNotification(
        [SnsTrigger(Route = "webhooks/sns")] SnsNotification notification,
        ILogger log)
    {
        log.LogInformation("=== SNS Notification Received ===");
        log.LogInformation("Message ID: {MessageId}", notification.MessageId);
        log.LogInformation("Topic ARN: {TopicArn}", notification.TopicArn);
        log.LogInformation("Subject: {Subject}", notification.Subject);
        log.LogInformation("Message: {Message}", notification.Message);
        log.LogInformation("Timestamp: {Timestamp}", notification.Timestamp);
        
        // For typed deserialization, use:
        // var orderEvent = notification.GetMessage<OrderCreatedEvent>();
        
        await Task.CompletedTask;
        log.LogInformation("SNS notification processed successfully");
    }

    #endregion

    #region Output Functions

    /// <summary>
    /// Publishes a message to an SNS topic using output binding.
    /// Example: curl -X POST "http://localhost:7071/api/sns/publish?message=Hello"
    /// </summary>
    [FunctionName(nameof(PublishToSns))]
    public async Task<IActionResult> PublishToSns(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "sns/publish")] HttpRequest req,
        [SnsOut(TopicArn = "%SNS_TOPIC_ARN%")] IAsyncCollector<SnsMessage> messages,
        ILogger log)
    {
        var messageText = req.Query["message"].ToString();
        if (string.IsNullOrEmpty(messageText))
        {
            messageText = "Hello from Azure Functions!";
        }

        await messages.AddAsync(new SnsMessage
        {
            Message = messageText,
            Subject = "Test Notification"
        });

        log.LogInformation("Published message to SNS: {Message}", messageText);

        return new OkObjectResult(new
        {
            status = "Message published to SNS",
            message = messageText
        });
    }

    /// <summary>
    /// Publishes a message with attributes to SNS.
    /// </summary>
    [FunctionName(nameof(PublishToSnsWithAttributes))]
    public async Task<IActionResult> PublishToSnsWithAttributes(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "sns/publish-with-attrs")] HttpRequest req,
        [SnsOut(TopicArn = "%SNS_TOPIC_ARN%")] IAsyncCollector<SnsMessage> messages,
        ILogger log)
    {
        var message = new SnsMessage
        {
            Message = "Order created notification",
            Subject = "Order Event",
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                ["EventType"] = new MessageAttributeValue { DataType = "String", StringValue = "OrderCreated" },
                ["Priority"] = new MessageAttributeValue { DataType = "Number", StringValue = "1" }
            }
        };

        await messages.AddAsync(message);

        log.LogInformation("Published message with attributes to SNS");

        return new OkObjectResult(new
        {
            status = "Message with attributes published to SNS"
        });
    }

    #endregion
}
