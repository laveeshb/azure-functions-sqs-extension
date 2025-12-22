"""Tests for SQS Output binding."""

import json
from unittest.mock import MagicMock, patch

import pytest

from azure_functions_sqs.output import SqsCollector, SqsOutput, SqsOutputOptions


class TestSqsOutputOptions:
    """Tests for SqsOutputOptions."""

    def test_default_values(self) -> None:
        """Test default option values."""
        options = SqsOutputOptions()

        assert options.delay_seconds == 0
        assert options.message_group_id is None
        assert options.use_content_based_deduplication is False

    def test_invalid_delay_seconds_negative(self) -> None:
        """Test that negative delay_seconds raises ValueError."""
        with pytest.raises(ValueError, match="delay_seconds must be between 0 and 900"):
            SqsOutputOptions(delay_seconds=-1)

    def test_invalid_delay_seconds_too_high(self) -> None:
        """Test that delay_seconds above 900 raises ValueError."""
        with pytest.raises(ValueError, match="delay_seconds must be between 0 and 900"):
            SqsOutputOptions(delay_seconds=901)


class TestSqsOutput:
    """Tests for SqsOutput binding."""

    def test_decorator_wraps_function(self) -> None:
        """Test that decorator properly wraps the function."""
        output = SqsOutput(queue_url="https://sqs.us-east-1.amazonaws.com/123/queue")

        with patch.object(output, "_send_message"):
            @output
            def my_function(x: int) -> str:
                return f"Result: {x}"

            result = my_function(42)

        assert result == "Result: 42"

    def test_return_value_sent_to_sqs(self) -> None:
        """Test that return value is sent to SQS."""
        output = SqsOutput(queue_url="https://sqs.us-east-1.amazonaws.com/123/queue")

        with patch.object(output, "_send_message") as mock_send:
            @output
            def my_function() -> str:
                return "Hello, SQS!"

            my_function()

        mock_send.assert_called_once_with("Hello, SQS!")

    def test_dict_return_value_serialized_to_json(self) -> None:
        """Test that dict return values are JSON serialized."""
        output = SqsOutput(queue_url="https://sqs.us-east-1.amazonaws.com/123/queue")

        with patch.object(output, "_get_client") as mock_get_client:
            mock_client = MagicMock()
            mock_get_client.return_value = mock_client
            mock_client.send_message.return_value = "msg-id"

            @output
            def my_function() -> dict:
                return {"key": "value", "count": 42}

            my_function()

        # Check that JSON was sent
        call_kwargs = mock_client.send_message.call_args[1]
        sent_body = call_kwargs["body"]
        parsed = json.loads(sent_body)
        assert parsed == {"key": "value", "count": 42}

    def test_none_return_value_not_sent(self) -> None:
        """Test that None return value is not sent."""
        output = SqsOutput(queue_url="https://sqs.us-east-1.amazonaws.com/123/queue")

        with patch.object(output, "_get_client") as mock_get_client:
            mock_client = MagicMock()
            mock_get_client.return_value = mock_client

            @output
            def my_function() -> None:
                return None

            my_function()

        mock_client.send_message.assert_not_called()


class TestSqsCollector:
    """Tests for SqsCollector (IAsyncCollector equivalent)."""

    def test_add_string_message(self) -> None:
        """Test adding string messages to collector."""
        collector = SqsCollector(queue_url="https://sqs.us-east-1.amazonaws.com/123/queue")

        collector.add("Message 1")
        collector.add("Message 2")

        assert len(collector._messages) == 2
        assert collector._messages[0] == "Message 1"

    def test_add_dict_message_serialized(self) -> None:
        """Test that dict messages are JSON serialized."""
        collector = SqsCollector(queue_url="https://sqs.us-east-1.amazonaws.com/123/queue")

        collector.add({"key": "value"})

        assert collector._messages[0] == '{"key": "value"}'

    def test_flush_sends_all_messages(self) -> None:
        """Test that flush sends all collected messages."""
        collector = SqsCollector(queue_url="https://sqs.us-east-1.amazonaws.com/123/queue")

        with patch.object(collector, "_get_client") as mock_get_client:
            mock_client = MagicMock()
            mock_get_client.return_value = mock_client
            mock_client.send_message_batch.return_value = {
                "Successful": [{"Id": "0"}, {"Id": "1"}, {"Id": "2"}],
                "Failed": [],
            }

            collector.add("Message 1")
            collector.add("Message 2")
            collector.add("Message 3")
            sent_count = collector.flush()

        assert sent_count == 3
        assert len(collector._messages) == 0  # Cleared after flush

    def test_flush_batches_by_10(self) -> None:
        """Test that flush sends in batches of 10 (SQS limit)."""
        collector = SqsCollector(queue_url="https://sqs.us-east-1.amazonaws.com/123/queue")

        with patch.object(collector, "_get_client") as mock_get_client:
            mock_client = MagicMock()
            mock_get_client.return_value = mock_client

            # First batch: 10 messages, second batch: 5 messages
            mock_client.send_message_batch.side_effect = [
                {"Successful": [{"Id": str(i)} for i in range(10)], "Failed": []},
                {"Successful": [{"Id": str(i)} for i in range(5)], "Failed": []},
            ]

            # Add 15 messages
            for i in range(15):
                collector.add(f"Message {i}")

            sent_count = collector.flush()

        assert sent_count == 15
        assert mock_client.send_message_batch.call_count == 2

    def test_flush_handles_failures(self) -> None:
        """Test that flush handles partial failures."""
        collector = SqsCollector(queue_url="https://sqs.us-east-1.amazonaws.com/123/queue")

        with patch.object(collector, "_get_client") as mock_get_client:
            mock_client = MagicMock()
            mock_get_client.return_value = mock_client
            mock_client.send_message_batch.return_value = {
                "Successful": [{"Id": "0"}, {"Id": "2"}],
                "Failed": [{"Id": "1", "Message": "Error sending"}],
            }

            collector.add("Message 1")
            collector.add("Message 2")
            collector.add("Message 3")
            sent_count = collector.flush()

        assert sent_count == 2  # Only 2 succeeded
