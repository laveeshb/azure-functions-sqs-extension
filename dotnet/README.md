# Azure Functions SQS Extension for .NET

Azure Functions bindings for Amazon Simple Queue Service (SQS) supporting both in-process and isolated worker hosting models.

## 📁 Repository Structure

```
dotnet/
├── src/              # Source code for both SDK packages
├── test/             # Test projects and sample applications
├── scripts/          # Build, test, and setup scripts
├── localstack/       # LocalStack testing infrastructure
└── docs/             # Documentation and migration guides
```

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
- **AWS SQS** queue (or LocalStack for local testing)

### Development Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Azure Functions Core Tools v4](https://docs.microsoft.com/azure/azure-functions/functions-run-local)
- [AWS CLI](https://aws.amazon.com/cli/) (for AWS or LocalStack)
- [Docker & Docker Compose](https://www.docker.com/) (for LocalStack testing)

**Quick install:** Run `./scripts/install-prereqs.sh` to install all prerequisites automatically.

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

For local development without connecting to AWS, we provide LocalStack integration. See the [LocalStack Testing Guide](./LOCALSTACK_TESTING.md) for complete setup instructions.

**Quick start:**
```bash
# Start LocalStack with test queues
./setup-localstack.sh

# Send test messages
./send-test-message.sh
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
./localstack/setup-localstack.sh

# 2. Update local.settings.json with LocalStack endpoint
# See docs/LOCALSTACK_TESTING.md for configuration details

# 3. Start your function app
cd samples/Extensions.SQS.Sample.v3
func start

# 4. Send test messages
./localstack/send-test-message.sh
```

📖 **Complete guide:** See [LocalStack Testing Guide](./docs/LOCALSTACK_TESTING.md) for detailed setup and usage.

# Isolated worker model
cd test/Extensions.SQS.Test.Isolated
func start
```

## Migration Guide

Migrating from the older `AzureFunctions.Extension.SQS` package? See the [Migration Guide](./docs/MIGRATION_TO_ISOLATED_WORKER.md) for detailed instructions.

## Building from Source

```bash
# Build both packages
./scripts/build.sh -c Release -p

# Build specific package
cd src/Azure.WebJobs.Extensions.SQS
dotnet build -c Release

cd src/Azure.Functions.Worker.Extensions.SQS
dotnet build -c Release
```

## Testing

```bash
# Run test applications
./scripts/ci-test.sh --queue-url "your-queue-url" --aws-access-key-id "your-key" --aws-secret-access-key "your-secret"
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
