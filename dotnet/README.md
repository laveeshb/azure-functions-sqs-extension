# Azure Functions AWS Extensions for .NET

Azure Functions bindings for AWS services supporting both in-process and isolated worker hosting models.

## 📁 Repository Structure

```
dotnet/
├── src/                                                    
│   ├── Azure.WebJobs.Extensions.SQS/           # In-process SQS (legacy)
│   ├── Azure.Functions.Worker.Extensions.SQS/  # Isolated SQS
│   ├── Azure.Functions.Worker.Extensions.AWS.Common/    # Shared utilities
│   ├── Azure.Functions.Worker.Extensions.EventBridge/   # EventBridge output
│   ├── Azure.Functions.Worker.Extensions.SNS/           # SNS output
│   ├── Azure.Functions.Worker.Extensions.S3/            # S3 output
│   └── Azure.Functions.Worker.Extensions.Kinesis/       # Kinesis output
├── test/             # Test projects and sample applications
├── scripts/          # Build, test, and setup scripts
├── localstack/       # LocalStack testing infrastructure & documentation
├── README.md         # Main documentation
└── MIGRATION_GUIDE.md # Migration guide from legacy packages
```

## Packages

### Isolated Worker Packages (Recommended)

| Package | Description | NuGet |
|---------|-------------|-------|
| **Extensions.Azure.Functions.Worker.SQS** | SQS trigger & output bindings | [![NuGet](https://img.shields.io/nuget/v/Extensions.Azure.Functions.Worker.SQS.svg)](https://www.nuget.org/packages/Extensions.Azure.Functions.Worker.SQS) |
| **Extensions.Azure.Functions.Worker.EventBridge** | EventBridge output (PutEvents) | Coming soon |
| **Extensions.Azure.Functions.Worker.SNS** | SNS output (Publish/PublishBatch) | Coming soon |
| **Extensions.Azure.Functions.Worker.S3** | S3 output (PutObject/GetObject) | Coming soon |
| **Extensions.Azure.Functions.Worker.Kinesis** | Kinesis output (PutRecord/PutRecords) | Coming soon |
| **Extensions.Azure.Functions.Worker.AWS.Common** | Shared credential & config utilities | Coming soon |

### In-Process Package (Legacy)

| Package | Hosting Model | NuGet | Documentation |
|---------|---------------|-------|---------------|
| **Extensions.Azure.WebJobs.SQS** | In-process | [![NuGet](https://img.shields.io/nuget/v/Extensions.Azure.WebJobs.SQS.svg)](https://www.nuget.org/packages/Extensions.Azure.WebJobs.SQS) | [README](./src/Azure.WebJobs.Extensions.SQS/README.md) |

> **Note:** The in-process model is [retiring November 2026](https://learn.microsoft.com/azure/azure-functions/migrate-version-3-version-4). New development targets the isolated worker model.

## Installation

### In-Process Model

```bash
dotnet add package Extensions.Azure.WebJobs.SQS
```

### Isolated Worker Model (Recommended)

```bash
dotnet add package Extensions.Azure.Functions.Worker.SQS
```

## Requirements

- **.NET 6.0 or .NET 8.0** (recommended)
- **Azure Functions v4** runtime
- **AWS SQS** queue (or LocalStack for local testing)

### Development Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Azure Functions Core Tools v4](https://docs.microsoft.com/azure/azure-functions/functions-run-local)
- [AWS CLI](https://aws.amazon.com/cli/) (for AWS or LocalStack)
- [Docker & Docker Compose](https://www.docker.com/) (for LocalStack testing)

**Quick install:**
- **Linux/macOS:** `./scripts/unix/install-prereqs.sh`
- **Windows:** `.\scripts\windows\install-prereqs.ps1`

## Features

| Feature | SQS | EventBridge | SNS | S3 | Kinesis |
|---------|-----|-------------|-----|-----|---------|
| Trigger Binding | ✅ | ❌ | ❌ | ❌ | ❌ |
| Output Binding | ✅ | ✅ | ✅ | ✅ | ✅ |
| AWS Credential Chain | ✅ | ✅ | ✅ | ✅ | ✅ |
| LocalStack Support | ✅ | ✅ | ✅ | ✅ | ✅ |
| Batch Operations | ✅ | ✅ | ✅ | ❌ | ✅ |
| Message/Event Parsing | ✅ | ✅ | ❌ | ✅ | ❌ |

> **Note:** For EventBridge, SNS, S3, and Kinesis triggers, configure these services to send events to SQS, then use the SQS trigger.

## Quick Start

### In-Process Model

#### Trigger Binding

```csharp
using Azure.WebJobs.Extensions.SQS;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Amazon.SQS.Model;

public class SqsFunctions
{
    [FunctionName("ProcessSQSMessage")]
    public void Run(
        [SqsQueueTrigger(QueueUrl = "%SQS_QUEUE_URL%")] Message message,
        ILogger log)
    {
        log.LogInformation("Received message: {MessageId}", message.MessageId);
        log.LogInformation("Body: {Body}", message.Body);
    }
}
```

#### Output Binding

> **⚠️ Security Note**: This example uses `AuthorizationLevel.Anonymous` for demonstration purposes only. In production, use `AuthorizationLevel.Function` or higher and require API keys or authentication to prevent unauthorized access.

```csharp
using Azure.WebJobs.Extensions.SQS;
using Microsoft.Azure.WebJobs;
using Microsoft.AspNetCore.Http;

public class SqsFunctions
{
    [FunctionName("SendSQSMessage")]
    public void Run(
        [HttpTrigger(AuthorizationLevel.Anonymous)] HttpRequest req,
        [SqsQueueOut(QueueUrl = "%SQS_OUTPUT_QUEUE_URL%")] out SqsQueueMessage outMessage)
    {
        outMessage = new SqsQueueMessage
        {
            Body = "Hello from Azure Functions!",
            QueueUrl = string.Empty
        };
    }
}
```

### Isolated Worker Model

#### Trigger Binding

```csharp
using Azure.Functions.Worker;
using Azure.Functions.Worker.Extensions.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;

public class SqsFunctions
{
    private readonly ILogger<SqsFunctions> _logger;

    public SqsFunctions(ILogger<SqsFunctions> logger)
    {
        _logger = logger;
    }

    [Function("ProcessSQSMessage")]
    public void Run(
        [SqsTrigger(QueueUrl = "%SQS_QUEUE_URL%")] Message message)
    {
        _logger.LogInformation("Received message: {MessageId}", message.MessageId);
        _logger.LogInformation("Body: {Body}", message.Body);
    }
}
```

#### Output Binding

> **⚠️ Security Note**: This example uses `AuthorizationLevel.Anonymous` for demonstration purposes only. In production, use `AuthorizationLevel.Function` or higher and require API keys or authentication to prevent unauthorized access.

```csharp
using Azure.Functions.Worker;
using Azure.Functions.Worker.Extensions.SQS;
using Azure.Functions.Worker.Http;
using Amazon.SQS;
using Amazon.SQS.Model;

public class SqsFunctions
{
    private readonly IAmazonSQS _sqsClient;

    public SqsFunctions(IAmazonSQS sqsClient)
    {
        _sqsClient = sqsClient;
    }

    [Function("SendSQSMessage")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        var queueUrl = Environment.GetEnvironmentVariable("SQS_OUTPUT_QUEUE_URL");
        
        await _sqsClient.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = queueUrl,
            MessageBody = "Hello from Azure Functions!"
        });

        return req.CreateResponse(System.Net.HttpStatusCode.OK);
    }
}
```

## Authentication

The extension supports multiple AWS credential methods using the AWS credential chain. The extension automatically tries credentials in the following order:

### 1. Environment Variables (Recommended for Azure Functions)

**Best for:** Production Azure Functions, local development

Set AWS credentials as environment variables. The extension automatically discovers them.

**Local Development** (`local.settings.json`):
```json
{
  "Values": {
    "AWS_ACCESS_KEY_ID": "your-access-key",
    "AWS_SECRET_ACCESS_KEY": "your-secret-key",
    "AWS_REGION": "us-east-1"
  }
}
```

**Azure Functions (Production)**:

Configure in **Application Settings** via Azure Portal:
1. Go to Function App → **Settings** → **Configuration**
2. Add application settings:
   - `AWS_ACCESS_KEY_ID` = your access key
   - `AWS_SECRET_ACCESS_KEY` = your secret key
   - `AWS_REGION` = your region

Or use Azure CLI:
```bash
az functionapp config appsettings set \
  --name <function-app-name> \
  --resource-group <resource-group> \
  --settings \
    AWS_ACCESS_KEY_ID=<key> \
    AWS_SECRET_ACCESS_KEY=<secret> \
    AWS_REGION=us-east-1
```

**Best Practice - Azure Key Vault** (Recommended for Production):

Store secrets in Azure Key Vault and reference them:

1. Create secrets in Azure Key Vault
2. Enable **System-assigned Managed Identity** on your Function App
3. Grant the identity "Get" permission on Key Vault secrets
4. Reference secrets in Application Settings:
   ```
   AWS_ACCESS_KEY_ID=@Microsoft.KeyVault(SecretUri=https://your-vault.vault.azure.net/secrets/AwsAccessKeyId/)
   AWS_SECRET_ACCESS_KEY=@Microsoft.KeyVault(SecretUri=https://your-vault.vault.azure.net/secrets/AwsSecretAccessKey/)
   ```

### 2. AWS Credentials File

**Best for:** Local development only

Create `~/.aws/credentials`:
```ini
[default]
aws_access_key_id = your-access-key
aws_secret_access_key = your-secret-key
region = us-east-1
```

### 3. IAM Roles

**Best for:** Running on AWS infrastructure (EC2, ECS, Lambda)

If your Azure Function runs on AWS infrastructure (hybrid scenarios), the extension automatically uses:
- ECS container credentials
- EC2 instance profile credentials

### 4. Explicit Credentials (Legacy)

**Best for:** Backward compatibility only (in-process model only)

⚠️ **Not recommended** - Use environment variables instead.

```csharp
[SqsQueueTrigger(
    AWSKeyId = "%AWS_KEY_ID%",
    AWSAccessKey = "%AWS_ACCESS_KEY%",
    QueueUrl = "%SQS_QUEUE_URL%")]
```

### Security Best Practices

✅ **DO:**
- Use Azure Key Vault for production secrets
- Use environment variables over hardcoded credentials
- Rotate credentials regularly
- Use IAM roles when possible
- Apply least-privilege IAM policies

❌ **DON'T:**
- Hardcode credentials in code
- Commit credentials to source control
- Use root AWS account credentials
- Share credentials across environments

## New AWS Extensions Usage (Isolated Worker)

### EventBridge - Send Events

```csharp
using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using Azure.Functions.Worker.Extensions.EventBridge;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

public class EventBridgeFunctions
{
    [Function("SendToEventBridge")]
    public async Task<HttpResponseData> SendEvent(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        var body = await new StreamReader(req.Body).ReadToEndAsync();

        using var client = EventBridgeClientFactory.Create(
            region: Environment.GetEnvironmentVariable("AWS_REGION"),
            serviceUrl: Environment.GetEnvironmentVariable("EVENTBRIDGE_SERVICE_URL"));

        var response = await client.PutEventsAsync(new PutEventsRequest
        {
            Entries = new List<PutEventsRequestEntry>
            {
                new()
                {
                    EventBusName = Environment.GetEnvironmentVariable("EVENT_BUS_NAME"),
                    Source = "azure.functions",
                    DetailType = "OrderCreated",
                    Detail = body,
                    Time = DateTime.UtcNow
                }
            }
        });

        return req.CreateResponse(
            response.FailedEntryCount == 0 ? HttpStatusCode.OK : HttpStatusCode.InternalServerError);
    }
}
```

### SNS - Publish Messages

```csharp
using Amazon.SimpleNotificationService.Model;
using Azure.Functions.Worker.Extensions.SNS;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

public class SnsFunctions
{
    [Function("PublishToSns")]
    public async Task<HttpResponseData> Publish(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        var body = await new StreamReader(req.Body).ReadToEndAsync();

        using var client = SnsClientFactory.Create(
            region: Environment.GetEnvironmentVariable("AWS_REGION"),
            serviceUrl: Environment.GetEnvironmentVariable("SNS_SERVICE_URL"));

        // Using builder for message attributes (enables subscriber filtering)
        var request = new SnsMessageBuilder()
            .WithTopicArn(Environment.GetEnvironmentVariable("SNS_TOPIC_ARN")!)
            .WithMessage(body)
            .WithSubject("New Order")
            .WithAttribute("eventType", "OrderCreated")
            .WithAttribute("priority", "high")
            .Build();

        var response = await client.PublishAsync(request);

        var httpResponse = req.CreateResponse(HttpStatusCode.OK);
        await httpResponse.WriteAsJsonAsync(new { messageId = response.MessageId });
        return httpResponse;
    }
}
```

### S3 - Upload Objects

```csharp
using Amazon.S3;
using Amazon.S3.Model;
using Azure.Functions.Worker.Extensions.S3;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

public class S3Functions
{
    [Function("UploadToS3")]
    public async Task<HttpResponseData> Upload(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "upload/{filename}")] HttpRequestData req,
        string filename)
    {
        using var client = S3ClientFactory.Create(
            region: Environment.GetEnvironmentVariable("AWS_REGION"),
            serviceUrl: Environment.GetEnvironmentVariable("S3_SERVICE_URL"));

        var response = await client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = Environment.GetEnvironmentVariable("S3_BUCKET_NAME"),
            Key = $"uploads/{filename}",
            InputStream = req.Body,
            ContentType = req.Headers.TryGetValues("Content-Type", out var ct) ? ct.First() : "application/octet-stream"
        });

        var httpResponse = req.CreateResponse(HttpStatusCode.OK);
        await httpResponse.WriteAsJsonAsync(new { etag = response.ETag });
        return httpResponse;
    }

    /// <summary>
    /// Process S3 events via SQS trigger (S3 -> SQS -> this function)
    /// </summary>
    [Function("ProcessS3Event")]
    public void ProcessS3Event([SqsTrigger("%S3_EVENTS_QUEUE_URL%")] Message message)
    {
        var notification = S3EventNotification.Parse(message.Body);

        foreach (var record in notification?.Records ?? [])
        {
            var bucketName = record.S3?.Bucket?.Name;
            var objectKey = record.S3?.Object?.Key;
            Console.WriteLine($"S3 Event: {record.EventName} - {bucketName}/{objectKey}");
        }
    }
}
```

### Kinesis - Send Records

```csharp
using Azure.Functions.Worker.Extensions.Kinesis;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

public class KinesisFunctions
{
    private readonly ILogger<KinesisFunctions> _logger;

    public KinesisFunctions(ILogger<KinesisFunctions> logger) => _logger = logger;

    [Function("SendToKinesis")]
    public async Task<HttpResponseData> SendRecord(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        var body = await new StreamReader(req.Body).ReadToEndAsync();

        using var client = KinesisClientFactory.Create(
            region: Environment.GetEnvironmentVariable("AWS_REGION"),
            serviceUrl: Environment.GetEnvironmentVariable("KINESIS_SERVICE_URL"));

        // Use entity ID as partition key for ordered processing
        var record = new KinesisRecordBuilder()
            .WithPartitionKey(Guid.NewGuid().ToString())
            .WithData(body)
            .Build();

        var response = await client.PutRecordsAsync(new Amazon.Kinesis.Model.PutRecordsRequest
        {
            StreamName = Environment.GetEnvironmentVariable("KINESIS_STREAM_NAME"),
            Records = new List<Amazon.Kinesis.Model.PutRecordsRequestEntry> { record }
        });

        var httpResponse = req.CreateResponse(HttpStatusCode.OK);
        await httpResponse.WriteAsJsonAsync(new { failedCount = response.FailedRecordCount });
        return httpResponse;
    }

    [Function("SendBatchToKinesis")]
    public async Task<HttpResponseData> SendBatch(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        using var client = KinesisClientFactory.Create();

        // KinesisBatchWriter handles 500-record limit automatically
        await using var writer = new KinesisBatchWriter(
            client,
            Environment.GetEnvironmentVariable("KINESIS_STREAM_NAME")!,
            _logger);

        for (int i = 0; i < 1000; i++)
        {
            await writer.AddAsync(new KinesisRecordBuilder()
                .WithRandomPartitionKey()
                .WithJsonData(new { index = i, timestamp = DateTime.UtcNow })
                .Build());
        }

        var httpResponse = req.CreateResponse(HttpStatusCode.OK);
        await httpResponse.WriteAsJsonAsync(new { status = "batch_sent", count = 1000 });
        return httpResponse;
    }
}
```

## Configuration

Configure polling behavior in `host.json`:

```json
{
  "version": "2.0",
  "extensions": {
    "sqsQueue": {
      "maxNumberOfMessages": 10,
      "pollingInterval": "00:00:15",
      "visibilityTimeout": "00:00:30"
    }
  }
}
```

### Configuration Options

| Setting | Description | Default |
|---------|-------------|---------|
| `maxNumberOfMessages` | Maximum number of messages to receive in a single batch (1-10) | 10 |
| `pollingInterval` | Time between polling attempts when queue is empty | 00:00:15 |
| `visibilityTimeout` | How long messages are invisible after being received | 00:00:30 |

## Local Development

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Azure Functions Core Tools v4](https://docs.microsoft.com/azure/azure-functions/functions-run-local)
- [AWS CLI](https://aws.amazon.com/cli/)
- Amazon SQS queue or [LocalStack](https://localstack.cloud/) for local testing

### Testing with LocalStack

For local development without connecting to AWS, we provide LocalStack integration. See the [LocalStack Testing Guide](./localstack/README.md) for complete setup instructions.

**Quick start:**
```bash
# Linux/macOS: Start LocalStack with test queues
./localstack/unix/setup-localstack.sh

# Windows: Start LocalStack with test queues
.\localstack\windows\setup-localstack.ps1

# Send test messages (Linux/macOS)
./localstack/unix/send-test-message.sh

# Send test messages (Windows)
.\localstack\windows\send-test-message.ps1
```

### Local Settings

Create a `local.settings.json` file:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "AWS_ACCESS_KEY_ID": "your-access-key",
    "AWS_SECRET_ACCESS_KEY": "your-secret-key",
    "AWS_REGION": "us-east-1",
    "SQS_QUEUE_URL": "https://sqs.us-east-1.amazonaws.com/123456789012/your-queue",
    "SQS_OUTPUT_QUEUE_URL": "https://sqs.us-east-1.amazonaws.com/123456789012/your-output-queue"
  }
}
```

### Running Locally

#### With AWS SQS

Configure your `local.settings.json` with AWS credentials (see above), then:

```bash
# In-process model
cd test/Extensions.SQS.Test.InProcess
func start

# Isolated worker model
cd test/Extensions.SQS.Test.Isolated
func start
```

#### With LocalStack (Recommended for Local Development)

No AWS credentials needed! LocalStack provides a local AWS environment:

```bash
# 1. Start LocalStack with test queues
# Linux/macOS:
./localstack/unix/setup-localstack.sh
# Windows:
.\localstack\windows\setup-localstack.ps1

# 2. Update local.settings.json with LocalStack endpoint
# See localstack/README.md for configuration details

# 3. Start your function app
cd test/Extensions.SQS.Test.Isolated
func start

# 4. Send test messages
# Linux/macOS:
./localstack/unix/send-test-message.sh
# Windows:
.\localstack\windows\send-test-message.ps1
```

📖 **Complete guide:** See [LocalStack Testing Guide](./localstack/README.md) for detailed setup and usage.

# Isolated worker model
cd test/Extensions.SQS.Test.Isolated
func start
```

## Migration Guide

Migrating from the older `AzureFunctions.Extension.SQS` package? See the [Migration Guide](./MIGRATION_GUIDE.md) for detailed instructions.

## Building from Source

```bash
# Build both packages
# Linux/macOS:
./scripts/unix/build.sh -c Release -p
# Windows:
.\scripts\windows\build.ps1 -Configuration Release -Package

# Build specific package
cd src/Azure.WebJobs.Extensions.SQS
dotnet build -c Release

cd src/Azure.Functions.Worker.Extensions.SQS
dotnet build -c Release
```

## Testing

```bash
# Run test applications
# Linux/macOS:
./scripts/unix/ci-test.sh --queue-url "your-queue-url" --aws-access-key-id "your-key" --aws-secret-access-key "your-secret"
# Windows:
.\scripts\windows\ci-test.ps1 -QueueUrl "your-queue-url" -AwsAccessKeyId "your-key" -AwsSecretAccessKey "your-secret"
```

## What's New

### v1.0.0 (December 2025)

- ✨ **Dual Package Architecture**: Separate packages for in-process and isolated worker models
- ✨ **.NET 6 & .NET 8** multi-targeting support
- ✨ **Azure Functions v4** support
- ✨ **AWS Credential Chain** support (no hardcoded credentials needed)
- ✨ **Nullable reference types** for better code safety
- ✨ **Modern async patterns** with proper cancellation token support
- ✨ **Enhanced error handling** and logging
- ✨ **Long polling** support (20-second wait time)
- ✨ **Improved resource disposal** patterns
- 📦 **Updated dependencies**: AWSSDK.SQS 3.7.0+, latest Azure Functions SDKs

## Support

For issues, questions, or feature requests, please [open an issue](https://github.com/laveeshb/azure-functions-sqs-extension/issues).
