"""SQS Trigger - polls SQS queue and invokes Azure Function."""

import asyncio
import logging
from dataclasses import dataclass, field
from datetime import timedelta
from typing import Any, Callable

from azure_functions_sqs.client import SqsClient
from azure_functions_sqs.message import SqsMessage

logger = logging.getLogger(__name__)


@dataclass
class SqsTriggerOptions:
    """
    SQS Trigger configuration options - matches .NET SqsQueueOptions.
    
    These can be set globally in host.json or per-function.
    """

    max_number_of_messages: int = 10
    """Maximum number of messages to retrieve in a single request (1-10). Default: 10."""

    visibility_timeout: timedelta = field(default_factory=lambda: timedelta(seconds=30))
    """Time that messages are hidden from other consumers after being retrieved. Default: 30s."""

    polling_interval: timedelta = field(default_factory=lambda: timedelta(seconds=5))
    """
    Delay between polling requests when the queue is empty.
    Note: SQS long polling (20s) already waits for messages, so this is an additional delay.
    Set to zero for immediate re-poll after long poll completes.
    Default: 5s.
    """


class SqsTrigger:
    """
    SQS Trigger binding for Azure Functions.
    
    Polls an SQS queue and invokes the decorated function for each message.
    Matches the .NET SqsQueueTriggerAttribute contract.
    
    Example:
        @app.function_name("ProcessSqsMessage")
        @SqsTrigger(
            queue_url="%SQS_QUEUE_URL%",
            region="%AWS_REGION%"
        )
        def process_message(message: SqsMessage):
            print(f"Received: {message.body}")
    """

    def __init__(
        self,
        queue_url: str,
        region: str | None = None,
        aws_key_id: str | None = None,
        aws_access_key: str | None = None,
        options: SqsTriggerOptions | None = None,
    ) -> None:
        """
        Initialize SQS Trigger.
        
        Args:
            queue_url: SQS Queue URL (required). Supports %ENV_VAR% syntax.
            region: AWS Region override. If not provided, extracted from queue_url.
            aws_key_id: AWS Access Key ID. Optional - uses credential chain if not provided.
            aws_access_key: AWS Secret Access Key. Optional - uses credential chain if not provided.
            options: Trigger options (polling interval, batch size, etc.).
        """
        self.queue_url = queue_url
        self.region = region
        self.aws_key_id = aws_key_id
        self.aws_access_key = aws_access_key
        self.options = options or SqsTriggerOptions()

        self._client: SqsClient | None = None
        self._running = False
        self._polling_task: asyncio.Task[None] | None = None

    def __call__(self, func: Callable[[SqsMessage], Any]) -> Callable[[SqsMessage], Any]:
        """Decorator to register the function as an SQS trigger handler."""
        self._handler = func
        return func

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

    async def start_async(self) -> None:
        """Start the polling loop."""
        if self._running:
            return

        self._running = True
        logger.info("Starting SQS trigger for queue: %s", self.queue_url)
        self._polling_task = asyncio.create_task(self._poll_loop_async())

    async def stop_async(self) -> None:
        """Stop the polling loop gracefully."""
        logger.info("Stopping SQS trigger for queue: %s", self.queue_url)
        self._running = False

        if self._polling_task:
            # Wait up to 30 seconds for current poll to complete
            try:
                await asyncio.wait_for(self._polling_task, timeout=30.0)
            except asyncio.TimeoutError:
                logger.warning("Polling task did not complete within 30 seconds")
                self._polling_task.cancel()
            except asyncio.CancelledError:
                pass

    async def _poll_loop_async(self) -> None:
        """
        Main polling loop.
        
        Uses sequential polling to prevent overlapping requests
        (same fix as .NET Issue #12).
        """
        client = self._get_client()

        while self._running:
            try:
                messages = await asyncio.to_thread(
                    client.receive_messages,
                    max_number_of_messages=self.options.max_number_of_messages,
                    visibility_timeout=int(self.options.visibility_timeout.total_seconds()),
                    wait_time_seconds=20,  # Long polling
                )

                if messages:
                    logger.debug(
                        "Received %d messages from queue: %s",
                        len(messages),
                        self.queue_url,
                    )
                    await self._process_messages_async(messages)
                    # Poll again immediately if messages were received
                else:
                    # No messages - wait before polling again
                    if self.options.polling_interval.total_seconds() > 0:
                        await asyncio.sleep(self.options.polling_interval.total_seconds())

            except asyncio.CancelledError:
                break
            except Exception as ex:
                logger.error(
                    "Error in polling loop for queue %s: %s. Retrying in 5 seconds.",
                    self.queue_url,
                    ex,
                )
                await asyncio.sleep(5)

        logger.debug("Polling loop exited for queue: %s", self.queue_url)

    async def _process_messages_async(self, messages: list[SqsMessage]) -> None:
        """Process a batch of messages."""
        client = self._get_client()

        for message in messages:
            try:
                logger.debug("Processing message ID: %s", message.message_id)

                # Invoke the handler
                result = self._handler(message)
                if asyncio.iscoroutine(result):
                    await result

                # Delete message on success (auto-complete)
                await asyncio.to_thread(client.delete_message, message.receipt_handle)
                logger.debug("Successfully processed and deleted message: %s", message.message_id)

            except Exception as ex:
                logger.error(
                    "Error processing message %s: %s. Message will return to queue.",
                    message.message_id,
                    ex,
                )
                # Don't delete - message will become visible again after visibility timeout
