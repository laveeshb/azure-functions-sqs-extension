"""SQS Client wrapper using boto3."""

import logging
import os
import re
from typing import Any

import boto3
from botocore.config import Config

from azure_functions_sqs.message import SqsMessage

logger = logging.getLogger(__name__)


class SqsClient:
    """
    AWS SQS Client wrapper using boto3.
    
    Provides methods for receiving, sending, and deleting SQS messages.
    Uses the AWS credential chain by default (environment variables, IAM roles, etc.)
    """

    def __init__(
        self,
        queue_url: str,
        region: str | None = None,
        aws_access_key_id: str | None = None,
        aws_secret_access_key: str | None = None,
    ) -> None:
        """
        Initialize SQS client.
        
        Args:
            queue_url: The SQS queue URL (required).
            region: AWS region override. If not provided, extracted from queue_url.
            aws_access_key_id: Optional AWS access key. Uses credential chain if not provided.
            aws_secret_access_key: Optional AWS secret key. Uses credential chain if not provided.
        """
        self.queue_url = self._resolve_env_var(queue_url)
        self.region = self._resolve_env_var(region) or self._extract_region_from_url(self.queue_url)
        
        # Resolve credentials from environment variable references
        access_key = self._resolve_env_var(aws_access_key_id)
        secret_key = self._resolve_env_var(aws_secret_access_key)

        # Warn if only one credential is provided
        if (access_key and not secret_key) or (secret_key and not access_key):
            logger.warning(
                "Only one AWS credential provided (access_key_id or secret_access_key). "
                "Both are required for explicit credentials. Falling back to credential chain."
            )

        # Build boto3 client
        config = Config(
            retries={"max_attempts": 3, "mode": "adaptive"},
        )

        client_kwargs: dict[str, Any] = {
            "service_name": "sqs",
            "region_name": self.region,
            "config": config,
        }

        # Only pass credentials if both are explicitly provided
        if access_key and secret_key:
            client_kwargs["aws_access_key_id"] = access_key
            client_kwargs["aws_secret_access_key"] = secret_key

        self._client = boto3.client(**client_kwargs)

    @staticmethod
    def _resolve_env_var(value: str | None) -> str | None:
        """
        Resolve environment variable references like %VAR_NAME% or ${VAR_NAME}.
        
        Matches .NET [AutoResolve] behavior.
        """
        if value is None:
            return None

        # Match %VAR_NAME% pattern (Azure Functions style)
        match = re.match(r"^%(.+)%$", value)
        if match:
            env_name = match.group(1)
            if env_name in os.environ:
                return os.environ[env_name]
            logger.warning(
                "Environment variable '%s' referenced in configuration but not set; "
                "using literal value '%s'.",
                env_name,
                value,
            )
            return value

        # Match ${VAR_NAME} pattern (shell style)
        match = re.match(r"^\$\{(.+)\}$", value)
        if match:
            env_name = match.group(1)
            if env_name in os.environ:
                return os.environ[env_name]
            logger.warning(
                "Environment variable '%s' referenced in configuration but not set; "
                "using literal value '%s'.",
                env_name,
                value,
            )
            return value

        return value

    @staticmethod
    def _extract_region_from_url(queue_url: str) -> str:
        """
        Extract AWS region from SQS queue URL.
        
        URL format: https://sqs.{region}.amazonaws.com/{account-id}/{queue-name}
        
        Raises:
            ValueError: If region cannot be extracted from URL.
        """
        # Standard AWS URL pattern
        match = re.search(r"sqs\.([a-z0-9-]+)\.amazonaws\.com", queue_url)
        if match:
            return match.group(1)

        # LocalStack pattern: http://localhost:4566 or similar
        if "localhost" in queue_url or "127.0.0.1" in queue_url:
            return os.environ.get("AWS_REGION", "us-east-1")

        raise ValueError(f"Unable to extract AWS region from queue URL: {queue_url}")

    def receive_messages(
        self,
        max_number_of_messages: int = 10,
        visibility_timeout: int = 30,
        wait_time_seconds: int = 20,
    ) -> list[SqsMessage]:
        """
        Receive messages from the SQS queue.
        
        Args:
            max_number_of_messages: Maximum messages to receive (1-10). Default: 10.
            visibility_timeout: Seconds messages are hidden after receive. Default: 30.
            wait_time_seconds: Long polling wait time (0-20). Default: 20.
            
        Returns:
            List of SqsMessage objects.
        """
        response = self._client.receive_message(
            QueueUrl=self.queue_url,
            MaxNumberOfMessages=max_number_of_messages,
            VisibilityTimeout=visibility_timeout,
            WaitTimeSeconds=wait_time_seconds,
            MessageAttributeNames=["All"],
            AttributeNames=["All"],
        )

        messages = response.get("Messages", [])
        return [SqsMessage.from_boto3(msg) for msg in messages]

    def delete_message(self, receipt_handle: str) -> None:
        """
        Delete a message from the queue.
        
        Args:
            receipt_handle: The receipt handle from the received message.
        """
        self._client.delete_message(
            QueueUrl=self.queue_url,
            ReceiptHandle=receipt_handle,
        )
        logger.debug("Deleted message with receipt handle: %s", receipt_handle[:20])

    def send_message(
        self,
        body: str,
        delay_seconds: int = 0,
        message_attributes: dict[str, dict[str, str]] | None = None,
        message_group_id: str | None = None,
        message_deduplication_id: str | None = None,
    ) -> str:
        """
        Send a message to the queue.
        
        Args:
            body: The message body.
            delay_seconds: Delay before message becomes visible (0-900). Default: 0.
            message_attributes: Optional message attributes.
            message_group_id: Required for FIFO queues.
            message_deduplication_id: Optional for FIFO queues with content-based dedup.
            
        Returns:
            The MessageId of the sent message.
        """
        kwargs: dict[str, Any] = {
            "QueueUrl": self.queue_url,
            "MessageBody": body,
            "DelaySeconds": delay_seconds,
        }

        if message_attributes:
            kwargs["MessageAttributes"] = message_attributes

        if message_group_id:
            kwargs["MessageGroupId"] = message_group_id

        if message_deduplication_id:
            kwargs["MessageDeduplicationId"] = message_deduplication_id

        response = self._client.send_message(**kwargs)
        message_id = response["MessageId"]
        logger.debug("Sent message with ID: %s", message_id)
        return message_id

    def send_message_batch(
        self,
        messages: list[dict[str, Any]],
    ) -> dict[str, Any]:
        """
        Send multiple messages to the queue in a batch.
        
        Args:
            messages: List of message entries. Each entry should have:
                - Id: Unique identifier for the message in this batch
                - MessageBody: The message body
                - Optional: DelaySeconds, MessageAttributes, MessageGroupId, etc.
                
        Returns:
            Response containing Successful and Failed message results.
        """
        response = self._client.send_message_batch(
            QueueUrl=self.queue_url,
            Entries=messages,
        )
        return response
