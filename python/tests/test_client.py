"""Tests for SqsClient."""

import os
from unittest.mock import MagicMock, patch

import pytest

from azure_functions_sqs.client import SqsClient


class TestSqsClientEnvVarResolution:
    """Tests for environment variable resolution."""

    def test_resolve_env_var_percent_syntax(self) -> None:
        """Test resolving %VAR_NAME% syntax."""
        os.environ["TEST_QUEUE_URL"] = "https://sqs.us-east-1.amazonaws.com/123/my-queue"

        result = SqsClient._resolve_env_var("%TEST_QUEUE_URL%")

        assert result == "https://sqs.us-east-1.amazonaws.com/123/my-queue"

    def test_resolve_env_var_dollar_syntax(self) -> None:
        """Test resolving ${VAR_NAME} syntax."""
        os.environ["TEST_REGION"] = "eu-west-1"

        result = SqsClient._resolve_env_var("${TEST_REGION}")

        assert result == "eu-west-1"

    def test_resolve_env_var_literal_value(self) -> None:
        """Test passing through literal values."""
        result = SqsClient._resolve_env_var("literal-value")

        assert result == "literal-value"

    def test_resolve_env_var_none(self) -> None:
        """Test handling None values."""
        result = SqsClient._resolve_env_var(None)

        assert result is None


class TestSqsClientRegionExtraction:
    """Tests for region extraction from queue URL."""

    def test_extract_region_standard_url(self) -> None:
        """Test extracting region from standard AWS URL."""
        url = "https://sqs.us-east-1.amazonaws.com/123456789/my-queue"

        result = SqsClient._extract_region_from_url(url)

        assert result == "us-east-1"

    def test_extract_region_different_regions(self) -> None:
        """Test extracting various region names."""
        test_cases = [
            ("https://sqs.eu-west-1.amazonaws.com/123/q", "eu-west-1"),
            ("https://sqs.ap-southeast-2.amazonaws.com/123/q", "ap-southeast-2"),
            ("https://sqs.us-gov-west-1.amazonaws.com/123/q", "us-gov-west-1"),
        ]

        for url, expected_region in test_cases:
            result = SqsClient._extract_region_from_url(url)
            assert result == expected_region, f"Failed for URL: {url}"

    def test_extract_region_localhost(self) -> None:
        """Test LocalStack URL falls back to AWS_REGION env var."""
        os.environ["AWS_REGION"] = "us-west-2"

        result = SqsClient._extract_region_from_url("http://localhost:4566/000000000000/test-queue")

        assert result == "us-west-2"

    def test_extract_region_invalid_url(self) -> None:
        """Test invalid URL raises ValueError."""
        with pytest.raises(ValueError, match="Unable to extract AWS region"):
            SqsClient._extract_region_from_url("https://invalid-url.com/queue")


class TestSqsClientOperations:
    """Tests for SQS operations using mocked boto3."""

    @patch("azure_functions_sqs.client.boto3")
    def test_receive_messages(self, mock_boto3: MagicMock) -> None:
        """Test receiving messages from SQS."""
        mock_client = MagicMock()
        mock_boto3.client.return_value = mock_client
        mock_client.receive_message.return_value = {
            "Messages": [
                {
                    "MessageId": "msg-1",
                    "ReceiptHandle": "receipt-1",
                    "Body": "Message 1",
                    "MD5OfBody": "md5-1",
                    "Attributes": {},
                    "MessageAttributes": {},
                },
                {
                    "MessageId": "msg-2",
                    "ReceiptHandle": "receipt-2",
                    "Body": "Message 2",
                    "MD5OfBody": "md5-2",
                    "Attributes": {},
                    "MessageAttributes": {},
                },
            ]
        }

        client = SqsClient(
            queue_url="https://sqs.us-east-1.amazonaws.com/123/test-queue"
        )
        messages = client.receive_messages()

        assert len(messages) == 2
        assert messages[0].message_id == "msg-1"
        assert messages[1].body == "Message 2"

    @patch("azure_functions_sqs.client.boto3")
    def test_delete_message(self, mock_boto3: MagicMock) -> None:
        """Test deleting a message from SQS."""
        mock_client = MagicMock()
        mock_boto3.client.return_value = mock_client

        client = SqsClient(
            queue_url="https://sqs.us-east-1.amazonaws.com/123/test-queue"
        )
        client.delete_message("receipt-handle-123")

        mock_client.delete_message.assert_called_once_with(
            QueueUrl="https://sqs.us-east-1.amazonaws.com/123/test-queue",
            ReceiptHandle="receipt-handle-123",
        )

    @patch("azure_functions_sqs.client.boto3")
    def test_send_message(self, mock_boto3: MagicMock) -> None:
        """Test sending a message to SQS."""
        mock_client = MagicMock()
        mock_boto3.client.return_value = mock_client
        mock_client.send_message.return_value = {"MessageId": "sent-msg-123"}

        client = SqsClient(
            queue_url="https://sqs.us-east-1.amazonaws.com/123/test-queue"
        )
        message_id = client.send_message(body="Test message")

        assert message_id == "sent-msg-123"
        mock_client.send_message.assert_called_once()

    @patch("azure_functions_sqs.client.boto3")
    def test_send_message_fifo(self, mock_boto3: MagicMock) -> None:
        """Test sending a message to FIFO queue."""
        mock_client = MagicMock()
        mock_boto3.client.return_value = mock_client
        mock_client.send_message.return_value = {"MessageId": "fifo-msg-123"}

        client = SqsClient(
            queue_url="https://sqs.us-east-1.amazonaws.com/123/test-queue.fifo"
        )
        client.send_message(
            body="FIFO message",
            message_group_id="group-1",
            message_deduplication_id="dedup-1",
        )

        call_kwargs = mock_client.send_message.call_args[1]
        assert call_kwargs["MessageGroupId"] == "group-1"
        assert call_kwargs["MessageDeduplicationId"] == "dedup-1"

    @patch("azure_functions_sqs.client.boto3")
    def test_send_message_fifo_requires_message_group_id(self, mock_boto3: MagicMock) -> None:
        """Test that sending to FIFO queue without message_group_id raises ValueError."""
        mock_client = MagicMock()
        mock_boto3.client.return_value = mock_client

        client = SqsClient(
            queue_url="https://sqs.us-east-1.amazonaws.com/123/test-queue.fifo"
        )

        with pytest.raises(ValueError, match="message_group_id is required for FIFO queues"):
            client.send_message(body="FIFO message without group ID")

    @patch("azure_functions_sqs.client.boto3")
    def test_receive_messages_invalid_max(self, mock_boto3: MagicMock) -> None:
        """Test that invalid max_number_of_messages raises ValueError."""
        mock_client = MagicMock()
        mock_boto3.client.return_value = mock_client

        client = SqsClient(
            queue_url="https://sqs.us-east-1.amazonaws.com/123/test-queue"
        )

        with pytest.raises(ValueError, match="max_number_of_messages must be between 1 and 10"):
            client.receive_messages(max_number_of_messages=15)

    @patch("azure_functions_sqs.client.boto3")
    def test_receive_messages_invalid_wait_time(self, mock_boto3: MagicMock) -> None:
        """Test that invalid wait_time_seconds raises ValueError."""
        mock_client = MagicMock()
        mock_boto3.client.return_value = mock_client

        client = SqsClient(
            queue_url="https://sqs.us-east-1.amazonaws.com/123/test-queue"
        )

        with pytest.raises(ValueError, match="wait_time_seconds must be between 0 and 20"):
            client.receive_messages(wait_time_seconds=25)

    @patch("azure_functions_sqs.client.boto3")
    def test_send_message_invalid_delay(self, mock_boto3: MagicMock) -> None:
        """Test that invalid delay_seconds raises ValueError."""
        mock_client = MagicMock()
        mock_boto3.client.return_value = mock_client

        client = SqsClient(
            queue_url="https://sqs.us-east-1.amazonaws.com/123/test-queue"
        )

        with pytest.raises(ValueError, match="delay_seconds must be between 0 and 900"):
            client.send_message(body="Test", delay_seconds=1000)
