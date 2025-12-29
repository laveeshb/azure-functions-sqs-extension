"""Azure Functions SQS Extension - AWS SQS bindings for Azure Functions."""

from __future__ import annotations

from azure_functions_sqs.message import MessageAttributeValue, SqsMessage
from azure_functions_sqs.output import SqsCollector, SqsOutput, SqsOutputOptions
from azure_functions_sqs.trigger import SqsTrigger, SqsTriggerOptions

__all__ = [
    "SqsMessage",
    "MessageAttributeValue",
    "SqsTrigger",
    "SqsTriggerOptions",
    "SqsOutput",
    "SqsOutputOptions",
    "SqsCollector",
]

__version__ = "1.0.0"
