# Azure Functions SQS Extension for Python

A Python package that enables Azure Functions to integrate with Amazon SQS (Simple Queue Service) for both trigger-based and output binding scenarios.

## 📁 Repository Structure

```
python/
├── azure_functions_sqs/   # Source code for the package
├── tests/                 # Unit tests
├── samples/               # Sample function app
├── pyproject.toml         # Package configuration
└── README.md              # This documentation
```

## Installation

```bash
pip install azure-functions-sqs
```

## Features

- **SQS Trigger**: Process messages from an SQS queue with automatic deletion on success
- **SQS Output Binding**: Send messages to SQS as a function return value
- **SQS Collector**: Batch send multiple messages efficiently (like `IAsyncCollector` in .NET)
- **FIFO Queue Support**: Full support for FIFO queues with message groups and deduplication
- **Environment Variable Resolution**: Use `%VAR_NAME%` or `${VAR_NAME}` syntax for credentials

## Quick Start

### SQS Trigger

Process messages from an SQS queue:

```python
from azure_functions_sqs import SqsTrigger, SqsMessage, SqsTriggerOptions
from datetime import timedelta

trigger = SqsTrigger(
    queue_url="%SQS_QUEUE_URL%",  # From environment variable
    aws_key_id="%AWS_ACCESS_KEY_ID%",
    aws_access_key="%AWS_SECRET_ACCESS_KEY%",
    options=SqsTriggerOptions(
        max_number_of_messages=10,
        visibility_timeout=timedelta(seconds=30),
        polling_interval=timedelta(seconds=5),
    ),
)

@trigger
def process_message(message: SqsMessage) -> None:
    print(f"Received: {message.body}")
    # Message is automatically deleted after successful processing
```

### SQS Output Binding

Send messages to SQS:

```python
from azure_functions_sqs import SqsOutput
import json

output = SqsOutput(
    queue_url="%OUTPUT_QUEUE_URL%",
    aws_key_id="%AWS_ACCESS_KEY_ID%",
    aws_access_key="%AWS_SECRET_ACCESS_KEY%",
)

@output
def send_to_sqs() -> str:
    return json.dumps({"key": "value"})
```

### Batch Sending with SqsCollector

Send multiple messages efficiently:

```python
from azure_functions_sqs import SqsCollector

collector = SqsCollector(
    queue_url="%OUTPUT_QUEUE_URL%",
    aws_key_id="%AWS_ACCESS_KEY_ID%",
    aws_access_key="%AWS_SECRET_ACCESS_KEY%",
)

# Add messages
for i in range(25):
    collector.add({"item": i})

# Flush sends in batches of 10 (SQS limit)
sent_count = collector.flush()
print(f"Sent {sent_count} messages")
```

## Configuration

### Trigger Options

| Option | Default | Description |
|--------|---------|-------------|
| `max_number_of_messages` | 10 | Number of messages to receive per poll (1-10) |
| `visibility_timeout` | 30 seconds | How long a message is hidden from other consumers |
| `polling_interval` | 5 seconds | Delay between polls when queue is empty |

### Credentials

The extension supports multiple credential sources:

1. **Environment variables** (recommended):
   - Use `%VAR_NAME%` or `${VAR_NAME}` syntax
   - Values are resolved at runtime

2. **Direct values** (for testing only):
   - Pass credentials directly (not recommended for production)

3. **IAM Role/Instance Profile**:
   - Leave credentials empty to use AWS default credential chain

### Example Environment Variables

```bash
# AWS credentials
export AWS_REGION=us-east-1
export AWS_ACCESS_KEY_ID=AKIA...
export AWS_SECRET_ACCESS_KEY=...

# Queue URLs
export SQS_QUEUE_URL=https://sqs.us-east-1.amazonaws.com/123456789012/my-queue
```

## Message Structure

The `SqsMessage` class provides access to all SQS message properties:

```python
@trigger
def process(message: SqsMessage) -> None:
    # Core properties
    print(message.message_id)
    print(message.body)
    print(message.receipt_handle)
    
    # Timestamps and counts
    print(message.sent_timestamp)
    print(message.approximate_receive_count)
    
    # FIFO queue properties
    print(message.message_group_id)
    print(message.message_deduplication_id)
    print(message.sequence_number)
    
    # Custom attributes
    for name, attr in message.message_attributes.items():
        print(f"{name}: {attr.string_value}")
    
    # Convert to dict (matches .NET JSON contract)
    data = message.to_dict()
```

## FIFO Queue Support

For FIFO queues (queue URL ends with `.fifo`):

```python
output = SqsOutput(
    queue_url="%FIFO_QUEUE_URL%",
    aws_key_id="%AWS_ACCESS_KEY_ID%",
    aws_access_key="%AWS_SECRET_ACCESS_KEY%",
    message_group_id="my-group",  # Required for FIFO
)
```

## LocalStack Testing

For local development with LocalStack:

```python
from azure_functions_sqs import SqsTrigger

trigger = SqsTrigger(
    queue_url="http://localhost:4566/000000000000/test-queue",
    region="us-east-1",  # Must specify for LocalStack
    aws_key_id="test",
    aws_access_key="test",
)
```

See the [LocalStack testing guide](../dotnet/localstack/README.md) for setting up LocalStack with Docker.

## Sample Application

A complete sample function app is available in the [`samples/`](./samples/) directory:

- [`function_app.py`](./samples/function_app.py) - Complete examples of trigger, output, and batch sending
- [`host.json`](./samples/host.json) - Azure Functions host configuration
- [`local.settings.json`](./samples/local.settings.json) - Local development settings template

## Building from Source

```bash
# Clone the repository
git clone https://github.com/laveeshb/azure-functions-sqs-extension.git
cd azure-functions-sqs-extension/python

# Create virtual environment
python -m venv .venv
source .venv/bin/activate  # Linux/macOS
# .venv\Scripts\activate   # Windows

# Install in development mode with test dependencies
pip install -e ".[dev]"

# Run tests
pytest tests/ -v
```

## Comparison with .NET Extension

This Python package is designed to match the .NET extension's contracts:

| .NET | Python |
|------|--------|
| `[SqsQueueTrigger]` | `@SqsTrigger()` |
| `[SqsQueueOut]` | `@SqsOutput()` |
| `IAsyncCollector<SqsQueueMessage>` | `SqsCollector` |
| `Amazon.SQS.Model.Message` | `SqsMessage` |
| `SqsQueueOptions` | `SqsTriggerOptions` |

## Requirements

- Python 3.9+
- boto3 >= 1.26.0
- azure-functions >= 1.17.0

## Support

For issues, questions, or feature requests, please [open an issue](https://github.com/laveeshb/azure-functions-sqs-extension/issues).

## License

MIT License - see LICENSE file for details.
