# Azure WebJobs SQS Extension

Amazon SQS extension for Azure Functions using the **In-Process (WebJobs) hosting model**.

> ℹ️ **Note:** For new projects, consider using [Azure.Functions.Worker.Extensions.SQS](https://www.nuget.org/packages/Azure.Functions.Worker.Extensions.SQS) which supports the **isolated worker model** (Microsoft's recommended approach). The in-process model will be [retired on November 10, 2026](https://aka.ms/azure-functions-retirements/in-process-model).

## Installation

```bash
dotnet add package Azure.Extensions.WebJobs.SQS
```

## Features

- ✅ Trigger functions from Amazon SQS queues
- ✅ Send messages to SQS queues via output bindings
- ✅ AWS credential chain support (no hardcoded credentials needed)
- ✅ Automatic message deletion on successful processing
- ✅ Configurable polling intervals and visibility timeouts
- ✅ Async/await support throughout
- ✅ .NET 6+ and .NET 8+ support

## Quick Start

### Prerequisites

- Azure Functions Runtime v4
- .NET 6 or .NET 8
- AWS Account with SQS queue
- AWS credentials configured (environment variables, IAM role, or AWS CLI)

### Trigger Example

```csharp
using Amazon.SQS.Model;
using Azure.WebJobs.Extensions.SQS;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

public class SqsFunctions
{
    [FunctionName("ProcessSqsMessage")]
    public void ProcessMessage(
        [SqsQueueTrigger(QueueUrl = "%SQS_QUEUE_URL%")] Message message,
        ILogger log)
    {
        log.LogInformation("Processing message: {MessageId}", message.MessageId);
        log.LogInformation("Message body: {Body}", message.Body);
        
        // Your processing logic here
        // Message is automatically deleted on success
    }
}
```

### Output Binding Example

> **⚠️ Security Note**: This example uses `AuthorizationLevel.Anonymous` for demonstration purposes only. In production, use `AuthorizationLevel.Function` or higher and require API keys or authentication to prevent unauthorized access.

```csharp
using Amazon.SQS.Model;
using Azure.WebJobs.Extensions.SQS;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

public class SqsOutputFunctions
{
    [FunctionName("SendMessage")]
    public async Task<IActionResult> SendMessage(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req,
        [SqsQueueOut(QueueUrl = "%SQS_OUTPUT_QUEUE_URL%")] IAsyncCollector<string> messages)
    {
        var message = await new StreamReader(req.Body).ReadToEndAsync();
        await messages.AddAsync(message);
        
        return new OkObjectResult(new { status = "Message sent" });
    }
}
```

## Configuration

### AWS Credentials

The extension uses the AWS credential chain to automatically discover credentials.

**For local development** - Use `local.settings.json`:
```json
{
  "Values": {
    "AWS_ACCESS_KEY_ID": "your-access-key",
    "AWS_SECRET_ACCESS_KEY": "your-secret-key",
    "AWS_REGION": "us-east-1"
  }
}
```

**For Azure Functions (production)** - Configure Application Settings:
- Via Azure Portal: Settings → Configuration → Application settings
- Via Azure CLI:
  ```bash
  az functionapp config appsettings set \
    --name <function-app-name> \
    --resource-group <resource-group> \
    --settings AWS_ACCESS_KEY_ID=<key> AWS_SECRET_ACCESS_KEY=<secret>
  ```

**Best Practice** - Use Azure Key Vault for production:
```
AWS_ACCESS_KEY_ID=@Microsoft.KeyVault(SecretUri=https://vault.azure.net/secrets/AwsAccessKeyId/)
AWS_SECRET_ACCESS_KEY=@Microsoft.KeyVault(SecretUri=https://vault.azure.net/secrets/AwsSecretAccessKey/)
```

The credential chain order:
1. Environment variables (recommended)
2. IAM roles (when running in AWS)
3. AWS CLI credentials file (`~/.aws/credentials`)

### local.settings.json

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "FUNCTIONS_INPROC_NET8_ENABLED": "1",
    "SQS_QUEUE_URL": "https://sqs.us-east-1.amazonaws.com/123456789012/my-queue",
    "SQS_OUTPUT_QUEUE_URL": "https://sqs.us-east-1.amazonaws.com/123456789012/my-output-queue",
    "AWS_REGION": "us-east-1"
  }
}
```

## Attributes

### SqsQueueTriggerAttribute

Triggers a function when messages are available in an SQS queue.

| Property | Type | Description | Default |
|----------|------|-------------|---------|
| `QueueUrl` | string | SQS queue URL (required) | - |
| `AWSKeyId` | string | AWS Access Key ID (optional) | null |
| `AWSAccessKey` | string | AWS Secret Access Key (optional) | null |
| `Region` | string | AWS Region (optional) | null |
| `MaxNumberOfMessages` | int | Max messages per batch (1-10) | 10 |
| `WaitTimeSeconds` | int | Long polling wait time (0-20) | 20 |
| `VisibilityTimeout` | int | Message visibility timeout (seconds) | 30 |
| `AutoDelete` | bool | Auto-delete on success | true |

### SqsQueueOutAttribute

Sends messages to an SQS queue.

| Property | Type | Description | Default |
|----------|------|-------------|---------|
| `QueueUrl` | string | SQS queue URL (required) | - |
| `AWSKeyId` | string | AWS Access Key ID (optional) | null |
| `AWSAccessKey` | string | AWS Secret Access Key (optional) | null |
| `Region` | string | AWS Region (optional) | null |

## Advanced Usage

### Binding to Message Object

```csharp
[FunctionName("ProcessFullMessage")]
public void ProcessFullMessage(
    [SqsQueueTrigger(QueueUrl = "%SQS_QUEUE_URL%")] Message message,
    ILogger log)
{
    log.LogInformation("Message ID: {Id}", message.MessageId);
    log.LogInformation("Receipt Handle: {Handle}", message.ReceiptHandle);
    log.LogInformation("Attributes: {Count}", message.Attributes.Count);
    
    foreach (var attr in message.MessageAttributes)
    {
        log.LogInformation("Attribute {Key}: {Value}", 
            attr.Key, attr.Value.StringValue);
    }
}
```

### Sending Multiple Messages

```csharp
[FunctionName("SendBatch")]
public async Task<IActionResult> SendBatch(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req,
    [SqsQueueOut(QueueUrl = "%SQS_OUTPUT_QUEUE_URL%")] IAsyncCollector<string> messages)
{
    for (int i = 0; i < 10; i++)
    {
        await messages.AddAsync($"Message {i + 1}");
    }
    
    return new OkObjectResult(new { sent = 10 });
}
```

### Sending with Message Attributes

```csharp
[FunctionName("SendWithAttributes")]
public async Task<IActionResult> SendWithAttributes(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req,
    [SqsQueueOut(QueueUrl = "%SQS_OUTPUT_QUEUE_URL%")] IAsyncCollector<SendMessageRequest> messages)
{
    var request = new SendMessageRequest
    {
        MessageBody = "Hello with attributes",
        DelaySeconds = 5,
        MessageAttributes = new Dictionary<string, MessageAttributeValue>
        {
            ["Priority"] = new MessageAttributeValue 
            { 
                DataType = "Number", 
                StringValue = "1" 
            },
            ["Source"] = new MessageAttributeValue 
            { 
                DataType = "String", 
                StringValue = "AzureFunctions" 
            }
        }
    };
    
    await messages.AddAsync(request);
    return new OkObjectResult(new { status = "sent" });
}
```

## Migration Guide

### From AzureFunctions.Extension.SQS (v2.x)

This package is the direct replacement for the deprecated `AzureFunctions.Extension.SQS` for in-process functions.

**No code changes needed!** Just update your package reference:

```xml
<!-- Old -->
<PackageReference Include="AzureFunctions.Extension.SQS" Version="2.0.0" />

<!-- New -->
<PackageReference Include="Azure.WebJobs.Extensions.SQS" Version="1.0.0" />
```

Update using statements if needed:
```csharp
// Old
using Azure.Functions.Extensions.SQS;

// New
using Azure.WebJobs.Extensions.SQS;
```

### To Isolated Worker Model

If you want to migrate to the **recommended** isolated worker model, use `Azure.Functions.Worker.Extensions.SQS` instead. See the [migration guide](https://github.com/laveeshb/azure-functions-sqs-extension/blob/main/dotnet/MIGRATION_GUIDE.md).

## Troubleshooting

### Messages Not Being Processed

1. Check AWS credentials are configured correctly
2. Verify queue URL is correct in configuration
3. Check IAM permissions for SQS actions (`sqs:ReceiveMessage`, `sqs:DeleteMessage`)
4. Review Azure Functions logs for errors

### Permission Issues

Ensure your AWS credentials have these permissions:
```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "sqs:ReceiveMessage",
        "sqs:DeleteMessage",
        "sqs:GetQueueAttributes",
        "sqs:SendMessage"
      ],
      "Resource": "arn:aws:sqs:*:*:*"
    }
  ]
}
```

## Links

- [GitHub Repository](https://github.com/laveeshb/azure-functions-sqs-extension)
- [NuGet Package](https://www.nuget.org/packages/Azure.WebJobs.Extensions.SQS)
- [Isolated Worker Package](https://www.nuget.org/packages/Azure.Functions.Worker.Extensions.SQS)
- [Report Issues](https://github.com/laveeshb/azure-functions-sqs-extension/issues)
