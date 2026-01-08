# ⚡ Azure Functions - AWS Extensions

![.NET 6.0 | 8.0](https://img.shields.io/badge/.NET%206.0%20|%208.0-512BD4?logo=dotnet&logoColor=white) [![NuGet - In-Process](https://img.shields.io/nuget/v/Extensions.Azure.WebJobs.SQS.svg?label=NuGet%20In-Process)](https://www.nuget.org/packages/Extensions.Azure.WebJobs.SQS) [![NuGet - Isolated Worker](https://img.shields.io/nuget/v/Extensions.Azure.Functions.Worker.SQS.svg?label=NuGet%20Isolated)](https://www.nuget.org/packages/Extensions.Azure.Functions.Worker.SQS)  
![Python 3.9+](https://img.shields.io/badge/Python%203.9+-3776AB?logo=python&logoColor=white) [![PyPI](https://img.shields.io/pypi/v/azure-functions-sqs.svg?label=PyPI)](https://pypi.org/project/azure-functions-sqs/)

Multi-language [Azure Functions](https://learn.microsoft.com/azure/azure-functions/) bindings for AWS services. 🔗

## 📋 Overview

This repository provides Azure Functions extensions to integrate with AWS event services. Build hybrid cloud solutions that bridge Azure Functions with the AWS ecosystem. 🚀

## 🎯 Supported AWS Services

| Service | Trigger | Input | Output | Description |
|---------|---------|-------|--------|-------------|
| **SQS** | ✅ Poll | ❌ | ✅ | Message queuing - poll queues and send messages |
| **EventBridge** | ✅ Webhook | ❌ | ✅ | Event routing - receive via API Destinations |
| **SNS** | ✅ Webhook | ❌ | ✅ | Pub/sub - receive via HTTPS subscriptions |
| **S3** | ❌ | ✅ | ✅ | Object storage - read/write objects to buckets |
| **Kinesis** | ✅ Poll | ❌ | ✅ | Streaming - poll streams and send records |

### Trigger Patterns

- **Poll-based (SQS, Kinesis):** Azure Functions polls AWS services directly using long-polling
- **Webhook-based (SNS, EventBridge):** AWS pushes events to your Azure Functions HTTP endpoint
  - **SNS:** Configure HTTPS subscription to your function URL
  - **EventBridge:** Use API Destinations to send events to your function URL

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
| **Extensions.Azure.Functions.Worker.EventBridge** | EventBridge trigger & output | Coming soon |
| **Extensions.Azure.Functions.Worker.SNS** | SNS trigger & output | Coming soon |
| **Extensions.Azure.Functions.Worker.S3** | S3 input & output bindings | Coming soon |
| **Extensions.Azure.Functions.Worker.Kinesis** | Kinesis trigger & output | Coming soon |
| **Extensions.Azure.Functions.Worker.AWS.Common** | Shared utilities | Coming soon |

### Legacy Package (In-Process)

| Package | Description | NuGet |
|---------|-------------|-------|
| **Extensions.Azure.WebJobs.SQS** | SQS trigger & output (in-process) | [![NuGet](https://img.shields.io/nuget/v/Extensions.Azure.WebJobs.SQS.svg)](https://www.nuget.org/packages/Extensions.Azure.WebJobs.SQS) |

> **Note:** The in-process model is [retiring November 2026](https://learn.microsoft.com/azure/azure-functions/migrate-version-3-version-4). New development targets the isolated worker model.

**Features:**
- ⚡ Trigger from SQS queues, SNS topics, EventBridge rules, Kinesis streams
- 📥 Read objects from S3 buckets with input binding
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
│   TRIGGERS (Events → Azure Functions)                              │
│   ────────────────────────────────────                             │
│   SQS Queue ─────────→ [Poll] ──────→ Azure Function               │
│   Kinesis Stream ────→ [Poll] ──────→ Azure Function               │
│   SNS Topic ─────────→ [Webhook] ───→ Azure Function               │
│   EventBridge Rule ──→ [Webhook] ───→ Azure Function               │
│                                                                     │
│   INPUT BINDINGS (Read from AWS)                                   │
│   ──────────────────────────────                                   │
│   S3 Bucket ─────────→ [GetObject] ─→ Azure Function               │
│                                                                     │
│   OUTPUT BINDINGS (Azure Functions → AWS)                          │
│   ───────────────────────────────────────                          │
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
