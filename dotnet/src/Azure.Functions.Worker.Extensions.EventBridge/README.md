# Azure Functions EventBridge Extension (Isolated Worker)

This package provides Azure Functions bindings for AWS EventBridge, enabling you to trigger functions from EventBridge events and send events to EventBridge event buses.

## Installation

```bash
dotnet add package Extensions.Azure.Functions.Worker.EventBridge
```

## Usage

### EventBridge Trigger

Receive EventBridge events via an SQS queue subscription:

```csharp
[Function("ProcessEventBridgeEvent")]
public void Run(
    [EventBridgeTrigger("https://sqs.us-east-1.amazonaws.com/123456789/my-eventbridge-queue",
        AWSKeyId = "%AWS_ACCESS_KEY_ID%",
        AWSAccessKey = "%AWS_SECRET_ACCESS_KEY%",
        Region = "us-east-1")] 
    EventBridgeEvent eventBridgeEvent,
    FunctionContext context)
{
    var logger = context.GetLogger("ProcessEventBridgeEvent");
    logger.LogInformation($"Received event from source: {eventBridgeEvent.Source}");
    logger.LogInformation($"Detail type: {eventBridgeEvent.DetailType}");
    logger.LogInformation($"Detail: {eventBridgeEvent.Detail}");
}
```

### Strongly-Typed Events

Use generic `EventBridgeEvent<T>` for type-safe detail access:

```csharp
public class OrderDetail
{
    public string OrderId { get; set; }
    public decimal Amount { get; set; }
}

[Function("ProcessOrderEvent")]
public void Run(
    [EventBridgeTrigger("https://sqs.us-east-1.amazonaws.com/123456789/order-events-queue",
        Region = "us-east-1",
        Source = "orders.service",
        DetailType = "OrderCreated")] 
    EventBridgeEvent<OrderDetail> orderEvent,
    FunctionContext context)
{
    var logger = context.GetLogger("ProcessOrderEvent");
    logger.LogInformation($"Order {orderEvent.Detail?.OrderId} created for ${orderEvent.Detail?.Amount}");
}
```

### EventBridge Output

Send events to an EventBridge event bus:

```csharp
[Function("SendToEventBridge")]
[EventBridgeOutput("my-event-bus",
    AWSKeyId = "%AWS_ACCESS_KEY_ID%",
    AWSAccessKey = "%AWS_SECRET_ACCESS_KEY%",
    Region = "us-east-1",
    Source = "my.application",
    DetailType = "OrderProcessed")]
public EventBridgeOutputEvent Run(
    [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
    FunctionContext context)
{
    return new EventBridgeOutputEvent
    {
        Source = "my.application",
        DetailType = "OrderProcessed",
        Detail = new { orderId = "12345", status = "completed" }
    };
}
```

## Configuration

### Trigger Attributes

| Property | Description | Required |
|----------|-------------|----------|
| QueueUrl | SQS queue URL receiving EventBridge events | Yes |
| Region | AWS region | No* |
| AWSKeyId | AWS Access Key ID | No* |
| AWSAccessKey | AWS Secret Access Key | No* |
| MaxNumberOfMessages | Max messages per batch (1-10) | No (default: 10) |
| WaitTimeSeconds | Long polling wait time (0-20) | No (default: 20) |
| VisibilityTimeout | Visibility timeout in seconds | No (default: 30) |
| EventPattern | Filter by event pattern | No |
| Source | Filter by event source | No |
| DetailType | Filter by detail type | No |

### Output Attributes

| Property | Description | Required |
|----------|-------------|----------|
| EventBusName | Event bus name or ARN | Yes |
| Region | AWS region | No* |
| AWSKeyId | AWS Access Key ID | No* |
| AWSAccessKey | AWS Secret Access Key | No* |
| Source | Default event source | No |
| DetailType | Default detail type | No |

\* If not provided, the AWS SDK credential chain will be used.

## Architecture

```
EventBridge Rule → SQS Queue → Azure Function (polls SQS) → Your Code
                                            ↓
Your Code → EventBridge Output → EventBridge Event Bus
```

## License

MIT
