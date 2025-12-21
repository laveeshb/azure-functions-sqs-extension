"""Azure Functions SQS Extension - AWS SQS bindings for Azure Functions."""

from azure_functions_sqs.message import SqsMessage
from azure_functions_sqs.trigger import SqsTrigger, SqsTriggerOptions
from azure_functions_sqs.output import SqsOutput, SqsOutputOptions
from azure_functions_sqs.client import SqsClient

__all__ = [
    "SqsMessage",
    "SqsTrigger",
    "SqsTriggerOptions",
    "SqsOutput",
    "SqsOutputOptions",
    "SqsClient",
]

__version__ = "1.0.0"
