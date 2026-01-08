# Azure.WebJobs.Extensions.SNS

Azure Functions WebJobs extension for Amazon SNS (Simple Notification Service). This package provides trigger and output bindings for SNS.

## Installation

```bash
dotnet add package Extensions.Azure.WebJobs.SNS
```

## Trigger Binding

The SNS trigger uses a **webhook pattern**. SNS pushes messages to your Azure Function's HTTP endpoint via HTTPS subscription.

### Setup

1. Deploy your Azure Function with an HTTP endpoint
2. Create an SNS HTTPS subscription pointing to your function URL
3. The trigger automatically handles subscription confirmation

### Usage (In-Process)

```csharp
using Azure.WebJobs.Extensions.SNS;

public class SnsFunctions
{
    [FunctionName("ProcessNotification")]
    public static async Task Run(
        [SnsTrigger(Route = "webhooks/sns")] SnsNotification notification,
        ILogger log)
    {
        log.LogInformation($"Received message from topic: {notification.TopicArn}");
        log.LogInformation($"Subject: {notification.Subject}");
        log.LogInformation($"Message: {notification.Message}");
        
        // Deserialize message to a strongly-typed object
        var orderEvent = notification.GetMessage<OrderCreatedEvent>();
        log.LogInformation($"Order ID: {orderEvent.OrderId}");
    }
}
```

### SnsNotification Properties

| Property | Type | Description |
|----------|------|-------------|
| `MessageId` | `string` | Unique message identifier |
| `TopicArn` | `string` | ARN of the SNS topic |
| `Subject` | `string` | Message subject (optional) |
| `Message` | `string` | Message body |
| `Timestamp` | `DateTime` | Message timestamp |
| `Type` | `string` | Notification type |
| `UnsubscribeUrl` | `string` | URL to unsubscribe |
| `MessageAttributes` | `Dictionary` | Custom message attributes |

### Typed Deserialization

```csharp
// Get message as strongly-typed object
var message = notification.GetMessage<MyMessageType>();

// Get raw message JSON
string json = notification.Message;
```

### Subscription Confirmation

The trigger automatically handles SNS subscription confirmation requests. When you create a new HTTPS subscription in SNS, the trigger will:
1. Detect the `SubscriptionConfirmation` message type
2. Automatically call the `SubscribeURL` to confirm the subscription
3. Return HTTP 200 to complete the handshake

## Output Binding

### Usage (In-Process)

```csharp
using Azure.WebJobs.Extensions.SNS;

public class SnsFunctions
{
    [FunctionName("PublishMessage")]
    [return: SnsOut(TopicArn = "arn:aws:sns:us-east-1:123456789:my-topic")]
    public static SnsMessage Run(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        return new SnsMessage
        {
            Message = "Hello from Azure Functions!",
            Subject = "Test Message"
        };
    }
}
```

### Usage (Isolated Worker)

```csharp
using Azure.Functions.Worker.Extensions.SNS;

public class SnsFunctions
{
    [Function("PublishMessage")]
    [SnsOutput("arn:aws:sns:us-east-1:123456789:my-topic")]
    public static SnsEvent Run(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        return new SnsEvent
        {
            Message = "Hello from Azure Functions!",
            Subject = "Test Message"
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
