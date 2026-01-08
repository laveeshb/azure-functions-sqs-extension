# ⚡ Azure Functions - AWS Extensions

![.NET 6.0 | 8.0](https://img.shields.io/badge/.NET%206.0%20|%208.0-512BD4?logo=dotnet&logoColor=white) [![NuGet - In-Process](https://img.shields.io/nuget/v/Extensions.Azure.WebJobs.SQS.svg?label=NuGet%20In-Process)](https://www.nuget.org/packages/Extensions.Azure.WebJobs.SQS) [![NuGet - Isolated Worker](https://img.shields.io/nuget/v/Extensions.Azure.Functions.Worker.SQS.svg?label=NuGet%20Isolated)](https://www.nuget.org/packages/Extensions.Azure.Functions.Worker.SQS)  
![Python 3.9+](https://img.shields.io/badge/Python%203.9+-3776AB?logo=python&logoColor=white) [![PyPI](https://img.shields.io/pypi/v/azure-functions-sqs.svg?label=PyPI)](https://pypi.org/project/azure-functions-sqs/)

Multi-language [Azure Functions](https://learn.microsoft.com/azure/azure-functions/) bindings for AWS services. 🔗

## 📋 Overview

This repository provides Azure Functions extensions to integrate with AWS event services. Build hybrid cloud solutions that bridge Azure Functions with the AWS ecosystem. 🚀

## 🎯 Supported AWS Services

| Service | Trigger | Output | Description |
|---------|---------|--------|-------------|
| **SQS** | ✅ | ✅ | Message queuing - poll queues and send messages |
| **EventBridge** | ❌ | ✅ | Event routing - publish events to event buses |
| **SNS** | ❌ | ✅ | Pub/sub - publish to topics for fan-out |
| **S3** | ❌ | ✅ | Object storage - upload objects to buckets |
| **Kinesis** | ❌ | ✅ | Streaming - send records to data streams |

> **Note:** For EventBridge, SNS, S3, and Kinesis triggers, configure these services to send events to SQS, then use the SQS trigger to receive them in Azure Functions.

## 🌐 Supported Languages

| Language | Status | Documentation |
|----------|--------|---------------|
| **.NET** | ✅ Available | [Documentation](./dotnet/README.md) |
| **Python** | ✅ Available (SQS only) | [Documentation](./python/README.md) |
| **Java** | 🚧 Coming soon | - |
| **JavaScript/TypeScript** | 🚧 Coming soon | - |

## 🔧 .NET Extensions

For .NET developers, this repository provides extensions for the **Isolated Worker** model:

### Available Packages

| Package | Description | NuGet |
|---------|-------------|-------|
| **Extensions.Azure.Functions.Worker.SQS** | SQS trigger & output bindings | [![NuGet](https://img.shields.io/nuget/v/Extensions.Azure.Functions.Worker.SQS.svg)](https://www.nuget.org/packages/Extensions.Azure.Functions.Worker.SQS) |
| **Extensions.Azure.Functions.Worker.EventBridge** | EventBridge output (PutEvents) | Coming soon |
| **Extensions.Azure.Functions.Worker.SNS** | SNS output (Publish/PublishBatch) | Coming soon |
| **Extensions.Azure.Functions.Worker.S3** | S3 output (PutObject/GetObject) | Coming soon |
| **Extensions.Azure.Functions.Worker.Kinesis** | Kinesis output (PutRecord/PutRecords) | Coming soon |
| **Extensions.Azure.Functions.Worker.AWS.Common** | Shared utilities | Coming soon |

### Legacy Package (In-Process)

| Package | Description | NuGet |
|---------|-------------|-------|
| **Extensions.Azure.WebJobs.SQS** | SQS trigger & output (in-process) | [![NuGet](https://img.shields.io/nuget/v/Extensions.Azure.WebJobs.SQS.svg)](https://www.nuget.org/packages/Extensions.Azure.WebJobs.SQS) |

> **Note:** The in-process model is [retiring November 2026](https://learn.microsoft.com/azure/azure-functions/migrate-version-3-version-4). New development targets the isolated worker model.

**Features:**
- ⚡ Trigger Azure Functions from SQS queue messages
- 📤 Send messages/events to SQS, SNS, EventBridge, S3, Kinesis
- 🎯 Multi-targeting: .NET 6.0 and .NET 8.0
- 🔐 AWS credential chain support
- 🔄 Long polling and configurable batch processing
- 🐳 LocalStack support for local development

📖 **[See full .NET documentation](./dotnet/README.md)**
🧪 **[LocalStack testing guide](./dotnet/localstack/README.md)**

## 🐍 Python Extension

For Python developers, install the native SQS package:

```bash
pip install azure-functions-sqs
```

**Features:**
- ⚡ `SqsTrigger` - Poll SQS queues with automatic message deletion
- 📤 `SqsOutput` - Send messages via function return values
- 📦 `SqsCollector` - Batch send multiple messages efficiently
- 🔐 AWS credential chain support (environment variables, IAM roles)
- 🐳 LocalStack support for local development

> **Note:** Python support for EventBridge, SNS, S3, and Kinesis is planned for future releases.

📖 **[See full Python documentation](./python/README.md)**

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                    COMPLETE AWS EVENT STORY                         │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│   INGEST (SQS Trigger)                                             │
│   ────────────────────                                             │
│   S3 Event ─────→ SQS ──┐                                          │
│   SNS ──────────→ SQS ──┼──→ SQS Trigger ──→ Azure Function        │
│   EventBridge ──→ SQS ──┘                                          │
│                                                                     │
│   EMIT (Output Bindings)                                           │
│   ──────────────────────                                           │
│   Azure Function ──→ SQS Output ────────→ Queue consumers          │
│                  ──→ SNS Output ────────→ Fan-out (pub/sub)        │
│                  ──→ EventBridge Output ─→ Event routing           │
│                  ──→ S3 Output ─────────→ Object storage           │
│                  ──→ Kinesis Output ────→ Real-time streams        │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

## 📜 History & Attribution

This repository is a continuation of the SQS extension originally developed as part of the [azure-function-extensions-net](https://github.com/laveeshb/azure-function-extensions-net) repository. The code has been extracted with full commit history to support multi-language development and focused maintenance.

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request. 💡

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 💬 Support

For issues, questions, or feature requests, please [open an issue](https://github.com/laveeshb/azure-functions-sqs-extension/issues). We're here to help! 🙋
