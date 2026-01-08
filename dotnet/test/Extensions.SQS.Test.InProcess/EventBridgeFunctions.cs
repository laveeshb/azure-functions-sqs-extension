namespace Azure.Functions.Extensions.SQS.Test.InProcess;

using Azure.WebJobs.Extensions.EventBridge;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

/// <summary>
/// Sample functions demonstrating EventBridge trigger and output bindings.
/// </summary>
public class EventBridgeFunctions
{
    #region Trigger Functions

    /// <summary>
    /// EventBridge Webhook Trigger - receives events via API Destinations.
    /// 
    /// Setup:
    /// 1. Deploy this function to Azure
    /// 2. Create an EventBridge Connection with API key or OAuth auth
    /// 3. Create an API Destination pointing to: https://your-function.azurewebsites.net/api/webhooks/eventbridge
    /// 4. Create an EventBridge Rule that targets the API Destination
    /// </summary>
    [FunctionName(nameof(ProcessEventBridgeEvent))]
    public async Task ProcessEventBridgeEvent(
        [EventBridgeTrigger(Route = "webhooks/eventbridge")] EventBridgeEvent evt,
        ILogger log)
    {
        log.LogInformation("=== EventBridge Event Received ===");
        log.LogInformation("Event ID: {Id}", evt.Id);
        log.LogInformation("Source: {Source}", evt.Source);
        log.LogInformation("Detail Type: {DetailType}", evt.DetailType);
        log.LogInformation("Account: {Account}", evt.Account);
        log.LogInformation("Region: {Region}", evt.Region);
        log.LogInformation("Time: {Time}", evt.Time);
        log.LogInformation("Detail: {Detail}", evt.Detail);
        
        // For typed deserialization, use:
        // var orderEvent = evt.GetDetail<OrderCreatedEvent>();
        
        await Task.CompletedTask;
        log.LogInformation("EventBridge event processed successfully");
    }

    #endregion

    #region Output Functions

    /// <summary>
    /// Sends an event to EventBridge using output binding.
    /// Example: curl -X POST "http://localhost:7071/api/eventbridge/send"
    /// </summary>
    [FunctionName(nameof(SendToEventBridge))]
    public async Task<IActionResult> SendToEventBridge(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "eventbridge/send")] HttpRequest req,
        [EventBridgeOut(EventBusName = "%EVENTBRIDGE_BUS_NAME%")] IAsyncCollector<EventBridgeMessage> events,
        ILogger log)
    {
        var orderEvent = new
        {
            orderId = Guid.NewGuid().ToString(),
            customerId = "customer-123",
            amount = 99.99m,
            timestamp = DateTime.UtcNow
        };

        await events.AddAsync(new EventBridgeMessage
        {
            Source = "azure-functions.sample",
            DetailType = "OrderCreated",
            Detail = System.Text.Json.JsonSerializer.Serialize(orderEvent)
        });

        log.LogInformation("Sent event to EventBridge: {OrderId}", orderEvent.orderId);

        return new OkObjectResult(new
        {
            status = "Event sent to EventBridge",
            orderId = orderEvent.orderId
        });
    }

    /// <summary>
    /// Sends multiple events to EventBridge in batch.
    /// </summary>
    [FunctionName(nameof(SendBatchToEventBridge))]
    public async Task<IActionResult> SendBatchToEventBridge(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "eventbridge/send-batch")] HttpRequest req,
        [EventBridgeOut(EventBusName = "%EVENTBRIDGE_BUS_NAME%")] IAsyncCollector<EventBridgeMessage> events,
        ILogger log)
    {
        var count = int.TryParse(req.Query["count"], out var c) ? c : 3;
        
        for (int i = 0; i < count; i++)
        {
            await events.AddAsync(new EventBridgeMessage
            {
                Source = "azure-functions.sample",
                DetailType = "BatchEvent",
                Detail = $"{{\"index\": {i}, \"timestamp\": \"{DateTime.UtcNow:O}\"}}"
            });
        }

        log.LogInformation("Sent {Count} events to EventBridge", count);

        return new OkObjectResult(new
        {
            status = "Batch events sent to EventBridge",
            count
        });
    }

    #endregion
}
