# Azure Functions S3 Extension (Isolated Worker)

This package provides Azure Functions bindings for AWS S3 (Simple Storage Service), enabling you to trigger functions from S3 events and write objects to S3 buckets.

## Installation

```bash
dotnet add package Extensions.Azure.Functions.Worker.S3
```

## Usage

### S3 Trigger

React to S3 events via an SQS queue:

```csharp
[Function("ProcessS3Event")]
public void Run(
    [S3Trigger("https://sqs.us-east-1.amazonaws.com/123456789/my-s3-events-queue",
        AWSKeyId = "%AWS_ACCESS_KEY_ID%",
        AWSAccessKey = "%AWS_SECRET_ACCESS_KEY%",
        Region = "us-east-1")] 
    S3Event s3Event,
    FunctionContext context)
{
    var logger = context.GetLogger("ProcessS3Event");
    
    foreach (var record in s3Event.Records ?? Enumerable.Empty<S3EventRecord>())
    {
        logger.LogInformation($"Event: {record.EventName}");
        logger.LogInformation($"Bucket: {record.S3?.Bucket?.Name}");
        logger.LogInformation($"Key: {record.S3?.Object?.Key}");
        logger.LogInformation($"Size: {record.S3?.Object?.Size} bytes");
    }
}
```

### Single Record Binding

Bind directly to `S3EventRecord` for single-event processing:

```csharp
[Function("ProcessS3Object")]
public void Run(
    [S3Trigger("https://sqs.us-east-1.amazonaws.com/123456789/s3-queue",
        Region = "us-east-1",
        BucketName = "my-bucket",
        EventType = "s3:ObjectCreated:*",
        KeyPrefix = "uploads/",
        KeySuffix = ".json")] 
    S3EventRecord record,
    FunctionContext context)
{
    var logger = context.GetLogger("ProcessS3Object");
    logger.LogInformation($"New file uploaded: {record.S3?.Object?.Key}");
}
```

### S3 Output

Write objects to an S3 bucket:

```csharp
[Function("WriteToS3")]
[S3Output("my-bucket",
    AWSKeyId = "%AWS_ACCESS_KEY_ID%",
    AWSAccessKey = "%AWS_SECRET_ACCESS_KEY%",
    Region = "us-east-1",
    KeyPrefix = "outputs/")]
public S3OutputObject Run(
    [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
    FunctionContext context)
{
    return new S3OutputObject
    {
        Key = $"outputs/result-{DateTime.UtcNow:yyyyMMddHHmmss}.json",
        Content = "{ \"status\": \"success\" }",
        ContentType = "application/json",
        Metadata = new Dictionary<string, string>
        {
            ["processed-by"] = "azure-function"
        }
    };
}
```

## Configuration

### Trigger Attributes

| Property | Description | Required |
|----------|-------------|----------|
| QueueUrl | SQS queue URL receiving S3 events | Yes |
| Region | AWS region | No* |
| AWSKeyId | AWS Access Key ID | No* |
| AWSAccessKey | AWS Secret Access Key | No* |
| MaxNumberOfMessages | Max messages per batch (1-10) | No (default: 10) |
| WaitTimeSeconds | Long polling wait time (0-20) | No (default: 20) |
| VisibilityTimeout | Visibility timeout in seconds | No (default: 30) |
| BucketName | Filter by bucket name | No |
| EventType | Filter by event type | No |
| KeyPrefix | Filter by key prefix | No |
| KeySuffix | Filter by key suffix | No |

### Output Attributes

| Property | Description | Required |
|----------|-------------|----------|
| BucketName | Target S3 bucket name | Yes |
| Region | AWS region | No* |
| AWSKeyId | AWS Access Key ID | No* |
| AWSAccessKey | AWS Secret Access Key | No* |
| KeyPrefix | Default key prefix | No |
| ContentType | Default content type | No |

\* If not provided, the AWS SDK credential chain will be used.

## S3 Event Types

Common S3 event types you can filter on:
- `s3:ObjectCreated:*` - Any object creation
- `s3:ObjectCreated:Put` - PUT operations
- `s3:ObjectCreated:Post` - POST operations
- `s3:ObjectCreated:Copy` - Copy operations
- `s3:ObjectRemoved:*` - Any object deletion
- `s3:ObjectRemoved:Delete` - DELETE operations
- `s3:ObjectRestore:*` - Glacier restore events

## Architecture

```
S3 Bucket Event → SQS Queue → Azure Function (polls SQS) → Your Code
                                            ↓
Your Code → S3 Output → S3 Bucket
```

## License

MIT
