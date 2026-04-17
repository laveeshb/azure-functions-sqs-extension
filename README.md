# ⚡ Azure Functions - Amazon SQS Extension

![.NET 6.0 | 8.0](https://img.shields.io/badge/.NET%206.0%20|%208.0-512BD4?logo=dotnet&logoColor=white) [![NuGet - In-Process](https://img.shields.io/nuget/v/Extensions.Azure.WebJobs.SQS.svg?label=NuGet%20In-Process)](https://www.nuget.org/packages/Extensions.Azure.WebJobs.SQS) [![NuGet - Isolated Worker](https://img.shields.io/nuget/v/Extensions.Azure.Functions.Worker.SQS.svg?label=NuGet%20Isolated)](https://www.nuget.org/packages/Extensions.Azure.Functions.Worker.SQS)  
![Python 3.9+](https://img.shields.io/badge/Python%203.9+-3776AB?logo=python&logoColor=white) [![PyPI](https://img.shields.io/pypi/v/azure-functions-sqs.svg?label=PyPI)](https://pypi.org/project/azure-functions-sqs/)

Multi-language [Azure Functions](https://learn.microsoft.com/azure/azure-functions/) bindings for Amazon Simple Queue Service (SQS). 🔗

## 📋 Overview

This repository provides Azure Functions extensions to integrate with [Amazon SQS](https://aws.amazon.com/sqs/) across multiple programming languages. Trigger functions based on SQS queue messages or send messages to SQS queues from your Azure Functions. 🚀

## 🌐 Supported Languages

| Language | Status | Documentation |
|----------|--------|---------------|
| **.NET** | ✅ Available | [Documentation](./dotnet/README.md) |
| **Python** | ✅ Available | [Documentation](./python/README.md) |
| **Java** | 🚧 Coming soon | - |
| **JavaScript/TypeScript** | 🚧 Coming soon | - |

## 🔧 .NET Extension

For .NET developers, this extension provides two packages supporting both hosting models:

- **[Azure.WebJobs.Extensions.SQS](./dotnet/src/Azure.WebJobs.Extensions.SQS/README.md)** - Host-side extension. Install directly for the in-process model; isolated worker apps pick it up transitively.
- **[Azure.Functions.Worker.Extensions.SQS](./dotnet/src/Azure.Functions.Worker.Extensions.SQS/README.md)** - Worker-side extension. Install for the isolated worker (out-of-process) model.

**Features:**
- ⚡ Trigger Azure Functions from SQS queue messages
- 📤 Send messages to SQS queues from Azure Functions
- 🎯 Multi-targeting: .NET 6.0 and .NET 8.0
- 🔐 Password-less AWS auth via Entra ID federation (managed identity → AWS STS), plus AWS credential chain and explicit-key fallbacks
- 🔄 Long polling and configurable batch processing
- 🐳 LocalStack support for local development

📖 **[See full .NET documentation](./dotnet/README.md)**
🔐 **[Authentication guide](./docs/AUTHENTICATION.md)** — recommended setup uses managed identity, no AWS secret in Azure
🧪 **[LocalStack testing guide](./dotnet/localstack/README.md)**

## 🐍 Python Extension

For Python developers, install the native package:

```bash
pip install azure-functions-sqs
```

**Features:**
- ⚡ `SqsTrigger` - Poll SQS queues with automatic message deletion
- 📤 `SqsOutput` - Send messages via function return values
- 📦 `SqsCollector` - Batch send multiple messages efficiently
- 🔐 AWS credential chain support (environment variables, IAM roles)
- 🐳 LocalStack support for local development

📖 **[See full Python documentation](./python/README.md)**

## 📜 History & Attribution

This repository is a continuation of the SQS extension originally developed as part of the [azure-function-extensions-net](https://github.com/laveeshb/azure-function-extensions-net) repository. The code has been extracted with full commit history to support multi-language development and focused maintenance.

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request. 💡

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 💬 Support

For issues, questions, or feature requests, please [open an issue](https://github.com/laveeshb/azure-functions-sqs-extension/issues). We're here to help! 🙋
