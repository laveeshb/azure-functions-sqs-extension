# Azure.WebJobs.Extensions.EventBridge

Azure Functions WebJobs extension for Amazon EventBridge. This package provides trigger and output bindings for EventBridge.

## Installation

```bash
dotnet add package Extensions.Azure.WebJobs.EventBridge
```

## Trigger Binding

The EventBridge trigger uses a **webhook pattern** via AWS EventBridge API Destinations. EventBridge sends events to your Azure Function's HTTP endpoint.

### Setup

1. Deploy your Azure Function with an HTTP endpoint
2. Create an EventBridge API Destination pointing to your function URL
3. Create an EventBridge rule that routes events to the API Destination

### Usage (In-Process)

```csharp
using Azure.WebJobs.Extensions.EventBridge;

public class EventBridgeFunctions
{
    [FunctionName("ProcessEvent")]
    public static async Task Run(
        [EventBridgeTrigger(Route = "events/eventbridge")] EventBridgeEvent evt,
        ILogger log)
    {
        log.LogInformation($"Received event from source: {evt.Source}");
        log.LogInformation($"Detail type: {evt.DetailType}");
        
        // Deserialize the detail to a strongly-typed object
        var orderEvent = evt.GetDetail<OrderCreatedEvent>();
        log.LogInformation($"Order ID: {orderEvent.OrderId}");
    }
}
```

### EventBridgeEvent Properties

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | Unique event identifier |
| `Source` | `string` | Event source (e.g., "my-application") |
| `DetailType` | `string` | Event type description |
| `Detail` | `string` | JSON payload of the event |
| `Account` | `string` | AWS account ID |
| `Region` | `string` | AWS region |
| `Time` | `DateTime` | Event timestamp |
| `Resources` | `string[]` | Related AWS resources |

### Typed Deserialization

```csharp
// Get detail as strongly-typed object
var detail = evt.GetDetail<MyEventType>();

// Get raw detail JSON
string json = evt.Detail;
```

## Output Binding

### Usage (In-Process)

```csharp
using Azure.WebJobs.Extensions.EventBridge;

public class EventBridgeFunctions
{
    [FunctionName("SendEvent")]
    [return: EventBridgeOut(EventBusName = "my-event-bus", Region = "us-east-1")]
    public static EventBridgeMessage Run(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        return new EventBridgeMessage
        {
            Source = "my-application",
            DetailType = "order.created",
            Detail = "{\"orderId\": \"12345\"}"
        };
    }
}
```

### Usage (Isolated Worker)

```csharp
using Azure.Functions.Worker.Extensions.EventBridge;

public class EventBridgeFunctions
{
    [Function("SendEvent")]
    [EventBridgeOutput("my-event-bus", Region = "us-east-1")]
    public static EventBridgeEvent Run(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        return new EventBridgeEvent
        {
            Source = "my-application",
            DetailType = "order.created",
            Detail = "{\"orderId\": \"12345\"}"
        };
    }
}
```

## Configuration

Configure AWS credentials in `local.settings.json`:

```json
{
  "Values": {
    "AWS_ACCESS_KEY_ID": "your-access-key",
    "AWS_SECRET_ACCESS_KEY": "your-secret-key",
    "AWS_REGION": "us-east-1"
  }
}
```

Or use the attribute properties:
- `AWSKeyId` - AWS Access Key ID
- `AWSAccessKey` - AWS Secret Access Key
- `Region` - AWS Region
