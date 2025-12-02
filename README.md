# Azure Functions - Amazon SQS Extension

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Multi-language Azure Functions bindings for Amazon Simple Queue Service (SQS).

## Overview

This repository provides Azure Functions extensions to integrate with [Amazon SQS](https://aws.amazon.com/sqs/) across multiple programming languages. Trigger functions based on SQS queue messages or send messages to SQS queues from your Azure Functions.

## Supported Languages

- ✅ **.NET** - Full support with trigger and output bindings
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

### Features

| Binding Type | Description | Example |
|--------------|-------------|---------|
| Trigger | Trigger an Azure Function based on messages in an AWS SQS queue | [Sample](./dotnet/samples/Extensions.SQS.Sample.v2/Trigger/QueueMessageTrigger.cs) |
| Output | Push messages to an AWS SQS queue from your Azure Function | [Sample](./dotnet/samples/Extensions.SQS.Sample.v3/Output/QueueMessageOutput.cs) |

### Authentication

The bindings support AWS credentials through:
- AWS credentials file
- Environment variables
- IAM roles (when running on AWS infrastructure)

## History & Attribution

This repository is a continuation of the SQS extension originally developed as part of the [azure-function-extensions-net](https://github.com/laveeshb/azure-function-extensions-net) repository. The code has been extracted with full commit history to support multi-language development and focused maintenance.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

For issues, questions, or feature requests, please [open an issue](https://github.com/laveeshb/azure-functions-sqs-extension/issues).
