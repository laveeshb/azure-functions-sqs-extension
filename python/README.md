# Azure Functions SQS Extension for Python

A Python package that enables Azure Functions to integrate with Amazon SQS (Simple Queue Service).

## Installation

```bash
pip install azure-functions-sqs
```

## Features

- **SqsTrigger** - Poll SQS queues with automatic message deletion on success
- **SqsOutput** - Send messages to SQS via function return value
- **SqsCollector** - Batch send multiple messages efficiently
- **FIFO Queue Support** - Message groups and deduplication
- **AWS Credential Chain** - Environment variables, IAM roles, or explicit credentials

## Quick Example

```python
from azure_functions_sqs import SqsTrigger, SqsMessage

trigger = SqsTrigger(
    queue_url="%SQS_QUEUE_URL%",
    aws_key_id="%AWS_ACCESS_KEY_ID%",
    aws_access_key="%AWS_SECRET_ACCESS_KEY%",
)

@trigger
def process_message(message: SqsMessage) -> None:
    print(f"Received: {message.body}")
    # Message is automatically deleted after successful processing
```

## More Examples

📦 **[Sample Application](https://github.com/laveeshb/azure-functions-sqs-extension/tree/main/python/samples)** - Working function app with trigger, output, and batch examples

🐳 **[LocalStack Guide](https://github.com/laveeshb/azure-functions-sqs-extension/tree/main/dotnet/localstack)** - Local development with Docker

## Requirements

- Python 3.9+
- boto3 >= 1.26.0
- azure-functions >= 1.17.0

## Links

- [GitHub Repository](https://github.com/laveeshb/azure-functions-sqs-extension)
- [Issue Tracker](https://github.com/laveeshb/azure-functions-sqs-extension/issues)
- [.NET Extension](https://github.com/laveeshb/azure-functions-sqs-extension/tree/main/dotnet)

## License

MIT
