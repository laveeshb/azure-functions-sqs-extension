# Azure Functions - Amazon SQS Extension

![.NET 6.0 | 8.0](https://img.shields.io/badge/6.0%20|%208.0-512BD4?logo=dotnet&label=.NET&logoColor=white)
[![NuGet - In-Process](https://img.shields.io/nuget/v/Azure.WebJobs.Extensions.SQS.svg?label=Azure.WebJobs.Extensions.SQS)](https://www.nuget.org/packages/Azure.WebJobs.Extensions.SQS)
[![NuGet - Isolated Worker](https://img.shields.io/nuget/v/Azure.Functions.Worker.Extensions.SQS.svg?label=Azure.Functions.Worker.Extensions.SQS)](https://www.nuget.org/packages/Azure.Functions.Worker.Extensions.SQS)

Multi-language Azure Functions bindings for Amazon Simple Queue Service (SQS).

## Overview

This repository provides Azure Functions extensions to integrate with [Amazon SQS](https://aws.amazon.com/sqs/) across multiple programming languages. Trigger functions based on SQS queue messages or send messages to SQS queues from your Azure Functions.

## Supported Languages

| Language | Status | Documentation |
|----------|--------|---------------|
| **.NET** | ✅ Available | [Documentation](./dotnet/README.md) |
| **Python** | 🚧 Coming soon | - |
| **Java** | 🚧 Coming soon | - |
| **JavaScript/TypeScript** | 🚧 Coming soon | - |

## .NET Extension

For .NET developers, this extension provides two packages supporting both hosting models:

- **[Azure.WebJobs.Extensions.SQS](./dotnet/src/Azure.WebJobs.Extensions.SQS/README.md)** - In-process hosting model
- **[Azure.Functions.Worker.Extensions.SQS](./dotnet/src/Azure.Functions.Worker.Extensions.SQS/README.md)** - Isolated worker (out-of-process) model

**Features:**
- Trigger Azure Functions from SQS queue messages
- Send messages to SQS queues from Azure Functions
- Multi-targeting: .NET 6.0 and .NET 8.0
- AWS credential chain support
- Long polling and configurable batch processing

📖 **[See full .NET documentation](./dotnet/README.md)**

## History & Attribution

This repository is a continuation of the SQS extension originally developed as part of the [azure-function-extensions-net](https://github.com/laveeshb/azure-function-extensions-net) repository. The code has been extracted with full commit history to support multi-language development and focused maintenance.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

For issues, questions, or feature requests, please [open an issue](https://github.com/laveeshb/azure-functions-sqs-extension/issues).
