# Azure Functions SQS Extension for .NET

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Azure Functions bindings for Amazon Simple Queue Service (SQS) supporting both in-process and isolated worker hosting models.

## Packages

This extension provides two separate packages following Microsoft's pattern for Azure Functions:

| Package | Hosting Model | NuGet | Documentation |
|---------|---------------|-------|---------------|
| **Azure.WebJobs.Extensions.SQS** | In-process | Coming soon | [README](./src/Azure.WebJobs.Extensions.SQS/README.md) |
| **Azure.Functions.Worker.Extensions.SQS** | Isolated worker | Coming soon | [README](./src/Azure.Functions.Worker.Extensions.SQS/README.md) |

## Installation

### In-Process Model

```bash
dotnet add package Azure.WebJobs.Extensions.SQS
```

### Isolated Worker Model (Recommended)

```bash
dotnet add package Azure.Functions.Worker.Extensions.SQS
```

## Requirements

- **.NET 6.0 or .NET 8.0** (recommended)
- **Azure Functions v4** runtime
- **AWS SQS** queue

## Features

| Feature | In-Process | Isolated Worker |
|---------|------------|-----------------|
| SQS Trigger Binding | ✅ | ✅ |
| SQS Output Binding | ✅ | ✅ |
| AWS Credential Chain | ✅ | ✅ |
| Long Polling | ✅ | ✅ |
| Batch Processing | ✅ | ✅ |
| Message Attributes | ✅ | ✅ |

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

The extension supports multiple AWS credential methods (in order of precedence):

1. **AWS Credential Chain** (Recommended)
   - Environment variables (`AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`)
   - AWS credentials file (`~/.aws/credentials`)
   - IAM roles (when running on AWS infrastructure)
   - ECS container credentials
   - EC2 instance profile

2. **Explicit Credentials** (for backward compatibility, in-process only)
   ```csharp
   [SqsQueueTrigger(
       AWSKeyId = "%AWS_KEY_ID%",
       AWSAccessKey = "%AWS_ACCESS_KEY%",
       QueueUrl = "%SQS_QUEUE_URL%")]
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
- AWS SQS queue or [LocalStack](https://localstack.cloud/) for local testing

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

```bash
# In-process model
cd test/Extensions.SQS.Test.InProcess
func start

# Isolated worker model
cd test/Extensions.SQS.Test.Isolated
func start
```

## Migration Guide

Migrating from the older `AzureFunctions.Extension.SQS` package? See the [Migration Guide](../MIGRATION_TO_ISOLATED_WORKER.md) for detailed instructions.

## Building from Source

```bash
# Build both packages
./build.sh -c Release -p

# Build specific package
cd src/Azure.WebJobs.Extensions.SQS
dotnet build -c Release

cd src/Azure.Functions.Worker.Extensions.SQS
dotnet build -c Release
```

## Testing

```bash
# Run test applications
./test.sh --queue-url "your-queue-url" --aws-access-key-id "your-key" --aws-secret-access-key "your-secret"
```

## What's New

### v1.0.0 (December 2024)

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

## License

This project is licensed under the MIT License - see the [LICENSE](../LICENSE) file for details.

