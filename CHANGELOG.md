# Changelog

All notable changes to this project are documented here.

This project follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and [Semantic Versioning](https://semver.org/spec/v2.0.0.html). The .NET and Python extensions are versioned independently.

---

## .NET

### 1.1.0 — 2026-04-14

#### Fixed
- Isolated worker model now functions end-to-end. Previously, the isolated worker SQS trigger failed at startup and later at runtime due to:
  - The host-side WebJobs extension not being registered — fixed by adding the `ExtensionInformation` assembly attribute ([#75](https://github.com/laveeshb/azure-functions-sqs-extension/issues/75), [#76](https://github.com/laveeshb/azure-functions-sqs-extension/pull/76))
  - Binding type name mismatch between worker (`sqsTrigger`) and host (`sqsQueueTrigger`) — fixed by renaming `SqsTriggerAttribute` to `SqsQueueTriggerAttribute` ([#75](https://github.com/laveeshb/azure-functions-sqs-extension/issues/75), [#76](https://github.com/laveeshb/azure-functions-sqs-extension/pull/76))
  - Host-side value provider returning raw `Amazon.SQS.Model.Message` instead of `ParameterBindingData` — fixed by detecting isolated mode and wrapping the message ([#78](https://github.com/laveeshb/azure-functions-sqs-extension/issues/78), [#79](https://github.com/laveeshb/azure-functions-sqs-extension/pull/79))

#### Changed
- Worker extension: `SqsTriggerAttribute` renamed to `SqsQueueTriggerAttribute`. Update `[SqsTrigger(...)]` usages to `[SqsQueueTrigger(...)]`. (The old name was non-functional — no runtime behavior change for working code.)

#### Added
- Round-trip tests covering the host-to-worker SQS message serialization contract ([#77](https://github.com/laveeshb/azure-functions-sqs-extension/issues/77))

### 1.0.0 — 2025-12-29
- Initial release of the Azure Functions SQS extension for .NET
- SQS trigger and output binding for the in-process model
- SQS trigger for the isolated worker model (non-functional — see 1.1.0)
- AWS credential chain support
- LocalStack support for local development

---

## Python

### 1.0.0 — 2025-12-30
- Initial release of the native Python SQS extension package
