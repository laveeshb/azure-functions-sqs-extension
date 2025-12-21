"""SQS Output binding - sends messages to SQS queue."""

import json
import logging
from dataclasses import dataclass, field
from typing import Any, Callable, TypeVar

from azure_functions_sqs.client import SqsClient

logger = logging.getLogger(__name__)

T = TypeVar("T")


@dataclass
class SqsOutputOptions:
    """
    SQS Output binding configuration options.
    
    Matches .NET SqsQueueOutAttribute properties.
    """

    delay_seconds: int = 0
    """Delay before message becomes visible (0-900 seconds). Default: 0."""

    # FIFO queue options
    message_group_id: str | None = None
    """Message group ID for FIFO queues. Required for FIFO queues."""

    use_content_based_deduplication: bool = False
    """If True, uses content-based deduplication (FIFO queues only)."""


class SqsOutput:
    """
    SQS Output binding for Azure Functions.
    
    Sends the function's return value to an SQS queue.
    Matches the .NET SqsQueueOutAttribute contract.
    
    Example:
        @app.route(route="send-message")
        @SqsOutput(
            queue_url="%SQS_OUTPUT_QUEUE_URL%",
            region="%AWS_REGION%"
        )
        def send_message(req: func.HttpRequest) -> str:
            return f"Message from request: {req.params.get('msg')}"
    """

    def __init__(
        self,
        queue_url: str,
        region: str | None = None,
        aws_key_id: str | None = None,
        aws_access_key: str | None = None,
        options: SqsOutputOptions | None = None,
    ) -> None:
        """
        Initialize SQS Output binding.
        
        Args:
            queue_url: SQS Queue URL (required). Supports %ENV_VAR% syntax.
            region: AWS Region override. If not provided, extracted from queue_url.
            aws_key_id: AWS Access Key ID. Optional - uses credential chain if not provided.
            aws_access_key: AWS Secret Access Key. Optional - uses credential chain if not provided.
            options: Output binding options (delay, FIFO settings, etc.).
        """
        self.queue_url = queue_url
        self.region = region
        self.aws_key_id = aws_key_id
        self.aws_access_key = aws_access_key
        self.options = options or SqsOutputOptions()

        self._client: SqsClient | None = None

    def __call__(self, func: Callable[..., T]) -> Callable[..., T]:
        """Decorator to wrap the function and send return value to SQS."""

        def wrapper(*args: Any, **kwargs: Any) -> T:
            result = func(*args, **kwargs)
            self._send_message(result)
            return result

        return wrapper

    def _get_client(self) -> SqsClient:
        """Get or create the SQS client."""
        if self._client is None:
            self._client = SqsClient(
                queue_url=self.queue_url,
                region=self.region,
                aws_access_key_id=self.aws_key_id,
                aws_secret_access_key=self.aws_access_key,
            )
        return self._client

    def _send_message(self, value: Any) -> None:
        """
        Send a message to the SQS queue.
        
        Args:
            value: The value to send. Will be JSON serialized if not a string.
        """
        if value is None:
            logger.debug("Skipping SQS output - return value is None")
            return

        client = self._get_client()

        # Convert to string
        if isinstance(value, str):
            body = value
        elif isinstance(value, (dict, list)):
            body = json.dumps(value)
        else:
            body = str(value)

        message_id = client.send_message(
            body=body,
            delay_seconds=self.options.delay_seconds,
            message_group_id=self.options.message_group_id,
        )

        logger.debug("Sent message to SQS queue %s with ID: %s", self.queue_url, message_id)


class SqsCollector:
    """
    Collector for sending multiple messages to SQS.
    
    Matches the .NET IAsyncCollector<T> pattern.
    
    Example:
        @app.route(route="send-batch")
        def send_batch(req: func.HttpRequest, collector: SqsCollector):
            collector.add("Message 1")
            collector.add("Message 2")
            collector.add({"key": "value"})
            # Messages are sent when collector.flush() is called or context exits
    """

    def __init__(
        self,
        queue_url: str,
        region: str | None = None,
        aws_key_id: str | None = None,
        aws_access_key: str | None = None,
    ) -> None:
        """Initialize the collector."""
        self.queue_url = queue_url
        self.region = region
        self.aws_key_id = aws_key_id
        self.aws_access_key = aws_access_key
        self._messages: list[str] = []
        self._client: SqsClient | None = None

    def _get_client(self) -> SqsClient:
        """Get or create the SQS client."""
        if self._client is None:
            self._client = SqsClient(
                queue_url=self.queue_url,
                region=self.region,
                aws_access_key_id=self.aws_key_id,
                aws_secret_access_key=self.aws_access_key,
            )
        return self._client

    def add(self, message: Any) -> None:
        """
        Add a message to the collector.
        
        Args:
            message: Message to add. Will be JSON serialized if not a string.
        """
        if isinstance(message, str):
            self._messages.append(message)
        elif isinstance(message, (dict, list)):
            self._messages.append(json.dumps(message))
        else:
            self._messages.append(str(message))

    def flush(self) -> int:
        """
        Send all collected messages to SQS in batches.
        
        Returns:
            Number of messages sent successfully.
        """
        if not self._messages:
            return 0

        client = self._get_client()
        sent_count = 0

        # SQS batch limit is 10 messages
        for i in range(0, len(self._messages), 10):
            batch = self._messages[i : i + 10]
            entries = [
                {"Id": str(idx), "MessageBody": msg} for idx, msg in enumerate(batch)
            ]

            response = client.send_message_batch(entries)
            sent_count += len(response.get("Successful", []))

            if response.get("Failed"):
                for failed in response["Failed"]:
                    logger.error(
                        "Failed to send message %s: %s",
                        failed["Id"],
                        failed.get("Message", "Unknown error"),
                    )

        self._messages.clear()
        logger.debug("Flushed %d messages to SQS queue: %s", sent_count, self.queue_url)
        return sent_count
