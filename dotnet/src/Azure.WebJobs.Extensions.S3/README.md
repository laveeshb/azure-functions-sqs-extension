# Azure.WebJobs.Extensions.S3

Azure Functions WebJobs extension for Amazon S3. This package provides input and output bindings for S3.

## Installation

```bash
dotnet add package Extensions.Azure.WebJobs.S3
```

## Input Binding

Read objects from S3 buckets directly in your function.

### Usage (In-Process)

```csharp
using Azure.WebJobs.Extensions.S3;

public class S3Functions
{
    // Read as string
    [FunctionName("ReadTextFile")]
    public static async Task RunString(
        [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req,
        [S3Input(BucketName = "my-bucket", Key = "data/config.json", Region = "us-east-1")] string content,
        ILogger log)
    {
        log.LogInformation($"File content: {content}");
    }

    // Read as byte array
    [FunctionName("ReadBinaryFile")]
    public static async Task RunBytes(
        [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req,
        [S3Input(BucketName = "my-bucket", Key = "data/image.png", Region = "us-east-1")] byte[] content,
        ILogger log)
    {
        log.LogInformation($"File size: {content.Length} bytes");
    }

    // Read as stream
    [FunctionName("ReadLargeFile")]
    public static async Task RunStream(
        [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req,
        [S3Input(BucketName = "my-bucket", Key = "data/large-file.zip", Region = "us-east-1")] Stream content,
        ILogger log)
    {
        log.LogInformation($"Stream length: {content.Length}");
    }

    // Read as S3Object (includes metadata)
    [FunctionName("ReadWithMetadata")]
    public static async Task RunObject(
        [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req,
        [S3Input(BucketName = "my-bucket", Key = "data/document.pdf", Region = "us-east-1")] S3Object obj,
        ILogger log)
    {
        log.LogInformation($"Key: {obj.Key}");
        log.LogInformation($"Content-Type: {obj.ContentType}");
        log.LogInformation($"ETag: {obj.ETag}");
        log.LogInformation($"Last Modified: {obj.LastModified}");
        
        // Access the content
        var bytes = obj.GetContentAsBytes();
        var text = obj.GetContentAsString();
    }
}
```

### S3Object Properties

| Property | Type | Description |
|----------|------|-------------|
| `Key` | `string` | Object key in the bucket |
| `BucketName` | `string` | Name of the S3 bucket |
| `ContentType` | `string` | MIME type of the object |
| `ContentLength` | `long` | Size in bytes |
| `ETag` | `string` | Entity tag for the object |
| `LastModified` | `DateTime?` | Last modification timestamp |
| `Metadata` | `Dictionary` | Custom metadata |
| `ContentStream` | `Stream` | Raw content stream |

### Dynamic Key Binding

Use route parameters to dynamically specify the S3 key:

```csharp
[FunctionName("GetDocument")]
public static async Task Run(
    [HttpTrigger(AuthorizationLevel.Function, "get", Route = "documents/{documentId}")] HttpRequest req,
    [S3Input(BucketName = "my-bucket", Key = "documents/{documentId}.pdf")] S3Object document,
    string documentId,
    ILogger log)
{
    log.LogInformation($"Retrieved document: {documentId}");
}
```

## Output Binding

### Usage (In-Process)

```csharp
using Azure.WebJobs.Extensions.S3;

public class S3Functions
{
    [FunctionName("UploadFile")]
    [return: S3Out(BucketName = "my-bucket", Region = "us-east-1")]
    public static S3Message Run(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        return new S3Message
        {
            Key = "uploads/file.txt",
            Content = "Hello from Azure Functions!",
            ContentType = "text/plain"
        };
    }
}
```

### Usage (Isolated Worker)

```csharp
using Azure.Functions.Worker.Extensions.S3;

public class S3Functions
{
    [Function("UploadFile")]
    [S3Output("my-bucket", Region = "us-east-1")]
    public static S3Object Run(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        return new S3Object
        {
            Key = "uploads/file.txt",
            Content = "Hello from Azure Functions!",
            ContentType = "text/plain"
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
