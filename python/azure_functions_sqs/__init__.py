"""Azure Functions SQS Extension - AWS SQS bindings for Azure Functions."""

from azure_functions_sqs.message import SqsMessage, MessageAttributeValue
from azure_functions_sqs.trigger import SqsTrigger, SqsTriggerOptions
from azure_functions_sqs.output import SqsOutput, SqsOutputOptions, SqsCollector
from azure_functions_sqs.client import SqsClient

__all__ = [
    "SqsMessage",
    "MessageAttributeValue",
    "SqsTrigger",
    "SqsTriggerOptions",
    "SqsOutput",
    "SqsOutputOptions",
    "SqsCollector",
    "SqsClient",
]

__version__ = "1.0.0"
