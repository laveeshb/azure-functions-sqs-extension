# Azure Functions - Amazon SQS Extension

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Multi-language Azure Functions bindings for Amazon Simple Queue Service (SQS).

## Overview

This repository provides Azure Functions extensions to integrate with [Amazon SQS](https://aws.amazon.com/sqs/) across multiple programming languages. Trigger functions based on SQS queue messages or send messages to SQS queues from your Azure Functions.

**Latest Update (v3.0.0)**: Fully modernized for .NET 6+ with enhanced credential management, async patterns, and Azure Functions v4 support.

## Supported Languages

- ✅ **.NET** - Full support with trigger and output bindings (.NET 6+, .NET 8)
- 🚧 **Python** - Coming soon
- 🚧 **Java** - Coming soon
- 🚧 **JavaScript/TypeScript** - Coming soon

## .NET Extension

### Installation

```bash
dotnet add package AzureFunctions.Extension.SQS
```

- [NuGet Package](https://www.nuget.org/packages/AzureFunctions.Extension.SQS)
- [.NET Documentation](./dotnet/README.md)
- [Release Notes](https://github.com/laveeshb/azure-functions-sqs-extension/releases)

### Requirements

- **.NET 6.0 or .NET 8.0** (recommended)
- **Azure Functions v4** runtime
- **AWS SQS** queue

### Features

| Binding Type | Description | Example |
|--------------|-------------|---------|
| Trigger | Trigger an Azure Function based on messages in an AWS SQS queue | [Sample](./dotnet/samples/Extensions.SQS.Sample.v3/Trigger/QueueMessageTrigger.cs) |
| Output | Push messages to an AWS SQS queue from your Azure Function | [Sample](./dotnet/samples/Extensions.SQS.Sample.v3/Output/QueueMessageOutput.cs) |

### Quick Start

#### Trigger Binding

```csharp
[FunctionName("ProcessSQSMessage")]
public static void Run(
    [SqsQueueTrigger(QueueUrl = "%SQS_QUEUE_URL%")] Message message,
    ILogger log)
{
    log.LogInformation("Received message: {MessageId}", message.MessageId);
    // Process message
}
```

#### Output Binding

```csharp
[FunctionName("SendSQSMessage")]
public static void Run(
    [HttpTrigger(AuthorizationLevel.Anonymous)] HttpRequest req,
    [SqsQueueOut(QueueUrl = "%SQS_OUTPUT_QUEUE_URL%")] out SqsQueueMessage outMessage)
{
    outMessage = new SqsQueueMessage
    {
        Body = "Hello from Azure Functions!",
        QueueUrl = string.Empty
    };
}
```

### Authentication

The extension supports multiple AWS credential methods (in order of precedence):

1. **AWS Credential Chain** (Recommended)
   - Environment variables (`AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`)
   - AWS credentials file (`~/.aws/credentials`)
   - IAM roles (when running on AWS infrastructure)
   - ECS container credentials
   - EC2 instance profile

2. **Explicit Credentials** (for backward compatibility)
   ```csharp
   [SqsQueueTrigger(
       AWSKeyId = "%AWS_KEY_ID%",
       AWSAccessKey = "%AWS_ACCESS_KEY%",
       QueueUrl = "%SQS_QUEUE_URL%")]
   ```

### Configuration

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

### What's New in v3.0

- ✨ **.NET 6 & .NET 8** multi-targeting support
- ✨ **Azure Functions v4** isolated worker model
- ✨ **AWS Credential Chain** support (no hardcoded credentials needed)
- ✨ **Nullable reference types** for better code safety
- ✨ **Modern async patterns** with proper cancellation token support
- ✨ **Enhanced error handling** and logging
- ✨ **Long polling** support (20-second wait time)
- ✨ **Improved resource disposal** patterns
- 📦 **Updated dependencies**: AWSSDK.SQS 3.7.x, latest Azure Functions SDKs

## History & Attribution

This repository is a continuation of the SQS extension originally developed as part of the [azure-function-extensions-net](https://github.com/laveeshb/azure-function-extensions-net) repository. The code has been extracted with full commit history to support multi-language development and focused maintenance.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

For issues, questions, or feature requests, please [open an issue](https://github.com/laveeshb/azure-functions-sqs-extension/issues).
