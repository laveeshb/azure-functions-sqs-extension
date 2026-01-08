# Azure Functions SNS Extension (Isolated Worker)

This package provides Azure Functions bindings for AWS SNS (Simple Notification Service), enabling you to trigger functions from SNS notifications and publish messages to SNS topics.

## Installation

```bash
dotnet add package Extensions.Azure.Functions.Worker.SNS
```

## Usage

### SNS Trigger

Receive SNS notifications via an SQS queue subscription:

```csharp
[Function("ProcessSnsNotification")]
public void Run(
    [SnsTrigger("https://sqs.us-east-1.amazonaws.com/123456789/my-sns-queue",
        AWSKeyId = "%AWS_ACCESS_KEY_ID%",
        AWSAccessKey = "%AWS_SECRET_ACCESS_KEY%",
        Region = "us-east-1")] 
    SnsNotification notification,
    FunctionContext context)
{
    var logger = context.GetLogger("ProcessSnsNotification");
    logger.LogInformation($"Received from topic: {notification.TopicArn}");
    logger.LogInformation($"Subject: {notification.Subject}");
    logger.LogInformation($"Message: {notification.Message}");
}
```

### Strongly-Typed Messages

Use generic `SnsNotification<T>` for type-safe message access:

```csharp
public class OrderMessage
{
    public string OrderId { get; set; }
    public string Status { get; set; }
}

[Function("ProcessOrderNotification")]
public void Run(
    [SnsTrigger("https://sqs.us-east-1.amazonaws.com/123456789/order-notifications",
        Region = "us-east-1",
        TopicArn = "arn:aws:sns:us-east-1:123456789:order-updates")] 
    SnsNotification<OrderMessage> notification,
    FunctionContext context)
{
    var logger = context.GetLogger("ProcessOrderNotification");
    logger.LogInformation($"Order {notification.Message?.OrderId} is now {notification.Message?.Status}");
}
```

### SNS Output

Publish messages to an SNS topic:

```csharp
[Function("PublishToSns")]
[SnsOutput("arn:aws:sns:us-east-1:123456789:my-topic",
    AWSKeyId = "%AWS_ACCESS_KEY_ID%",
    AWSAccessKey = "%AWS_SECRET_ACCESS_KEY%",
    Region = "us-east-1")]
public SnsOutputMessage Run(
    [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
    FunctionContext context)
{
    return new SnsOutputMessage
    {
        Message = new { orderId = "12345", status = "shipped" },
        Subject = "Order Update"
    };
}
```

## Configuration

### Trigger Attributes

| Property | Description | Required |
|----------|-------------|----------|
| QueueUrl | SQS queue URL receiving SNS notifications | Yes |
| Region | AWS region | No* |
| AWSKeyId | AWS Access Key ID | No* |
| AWSAccessKey | AWS Secret Access Key | No* |
| MaxNumberOfMessages | Max messages per batch (1-10) | No (default: 10) |
| WaitTimeSeconds | Long polling wait time (0-20) | No (default: 20) |
| VisibilityTimeout | Visibility timeout in seconds | No (default: 30) |
| TopicArn | Filter by specific topic ARN | No |
| SubjectFilter | Filter by message subject | No |

### Output Attributes

| Property | Description | Required |
|----------|-------------|----------|
| TopicArn | ARN of the SNS topic | Yes |
| Region | AWS region | No* |
| AWSKeyId | AWS Access Key ID | No* |
| AWSAccessKey | AWS Secret Access Key | No* |
| Subject | Default message subject | No |
| MessageGroupId | Message group ID for FIFO topics | No |

\* If not provided, the AWS SDK credential chain will be used.

## Architecture

```
SNS Topic → SQS Subscription → Azure Function (polls SQS) → Your Code
                                            ↓
Your Code → SNS Output → SNS Topic → Subscribers
```

## License

MIT
