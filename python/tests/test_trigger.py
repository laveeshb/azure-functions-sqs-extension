"""Tests for SQS Trigger."""

import asyncio
from datetime import timedelta
from unittest.mock import AsyncMock, MagicMock, patch

import pytest

from azure_functions_sqs.message import SqsMessage
from azure_functions_sqs.trigger import SqsTrigger, SqsTriggerOptions


class TestSqsTriggerOptions:
    """Tests for SqsTriggerOptions."""

    def test_default_values(self) -> None:
        """Test default option values match .NET defaults."""
        options = SqsTriggerOptions()

        assert options.max_number_of_messages == 10
        assert options.visibility_timeout == timedelta(seconds=30)
        assert options.polling_interval == timedelta(seconds=5)

    def test_custom_values(self) -> None:
        """Test custom option values."""
        options = SqsTriggerOptions(
            max_number_of_messages=5,
            visibility_timeout=timedelta(seconds=60),
            polling_interval=timedelta(seconds=15),
        )

        assert options.max_number_of_messages == 5
        assert options.visibility_timeout.total_seconds() == 60
        assert options.polling_interval.total_seconds() == 15

    def test_invalid_max_number_of_messages_too_low(self) -> None:
        """Test that max_number_of_messages below 1 raises ValueError."""
        with pytest.raises(ValueError, match="max_number_of_messages must be between 1 and 10"):
            SqsTriggerOptions(max_number_of_messages=0)

    def test_invalid_max_number_of_messages_too_high(self) -> None:
        """Test that max_number_of_messages above 10 raises ValueError."""
        with pytest.raises(ValueError, match="max_number_of_messages must be between 1 and 10"):
            SqsTriggerOptions(max_number_of_messages=15)

    def test_invalid_visibility_timeout_negative(self) -> None:
        """Test that negative visibility_timeout raises ValueError."""
        with pytest.raises(ValueError, match="visibility_timeout must be non-negative"):
            SqsTriggerOptions(visibility_timeout=timedelta(seconds=-1))

    def test_invalid_polling_interval_negative(self) -> None:
        """Test that negative polling_interval raises ValueError."""
        with pytest.raises(ValueError, match="polling_interval must be non-negative"):
            SqsTriggerOptions(polling_interval=timedelta(seconds=-5))


class TestSqsTrigger:
    """Tests for SqsTrigger."""

    def test_decorator_returns_original_function(self) -> None:
        """Test that decorator returns the original function."""
        trigger = SqsTrigger(queue_url="https://sqs.us-east-1.amazonaws.com/123/queue")

        @trigger
        def my_handler(message: SqsMessage) -> None:
            pass

        # The decorated function should be the original
        assert my_handler.__name__ == "my_handler"

    def test_trigger_stores_handler(self) -> None:
        """Test that trigger stores the handler function."""
        trigger = SqsTrigger(queue_url="https://sqs.us-east-1.amazonaws.com/123/queue")

        @trigger
        def my_handler(message: SqsMessage) -> None:
            pass

        assert trigger._handler == my_handler

    @pytest.mark.asyncio
    async def test_start_and_stop(self) -> None:
        """Test starting and stopping the trigger."""
        trigger = SqsTrigger(queue_url="https://sqs.us-east-1.amazonaws.com/123/queue")

        @trigger
        def handler(msg: SqsMessage) -> None:
            pass

        with patch.object(trigger, "_poll_loop_async", new_callable=AsyncMock):
            await trigger.start_async()
            assert trigger._running is True
            assert trigger._polling_task is not None

            await trigger.stop_async()
            assert trigger._running is False

    @pytest.mark.asyncio
    async def test_process_messages_calls_handler(self) -> None:
        """Test that messages are passed to the handler."""
        trigger = SqsTrigger(queue_url="https://sqs.us-east-1.amazonaws.com/123/queue")
        handler_mock = MagicMock()

        @trigger
        def handler(msg: SqsMessage) -> None:
            handler_mock(msg)

        message = SqsMessage(
            message_id="msg-1",
            receipt_handle="receipt-1",
            body="Test body",
            md5_of_body="md5",
        )

        with patch.object(trigger, "_get_client") as mock_client:
            mock_client.return_value.delete_message = MagicMock()
            await trigger._process_messages_async([message])

        handler_mock.assert_called_once()
        assert handler_mock.call_args[0][0].message_id == "msg-1"

    @pytest.mark.asyncio
    async def test_message_deleted_on_success(self) -> None:
        """Test that message is deleted after successful processing."""
        trigger = SqsTrigger(queue_url="https://sqs.us-east-1.amazonaws.com/123/queue")

        @trigger
        def handler(msg: SqsMessage) -> None:
            pass  # Success

        message = SqsMessage(
            message_id="msg-1",
            receipt_handle="receipt-to-delete",
            body="Test",
            md5_of_body="md5",
        )

        with patch.object(trigger, "_get_client") as mock_get_client:
            mock_client = MagicMock()
            mock_get_client.return_value = mock_client

            await trigger._process_messages_async([message])

            mock_client.delete_message.assert_called_once_with("receipt-to-delete")

    @pytest.mark.asyncio
    async def test_message_not_deleted_on_failure(self) -> None:
        """Test that message is NOT deleted when handler fails."""
        trigger = SqsTrigger(queue_url="https://sqs.us-east-1.amazonaws.com/123/queue")

        @trigger
        def handler(msg: SqsMessage) -> None:
            raise ValueError("Processing failed!")

        message = SqsMessage(
            message_id="msg-1",
            receipt_handle="receipt-1",
            body="Test",
            md5_of_body="md5",
        )

        with patch.object(trigger, "_get_client") as mock_get_client:
            mock_client = MagicMock()
            mock_get_client.return_value = mock_client

            await trigger._process_messages_async([message])

            # Message should NOT be deleted on failure
            mock_client.delete_message.assert_not_called()


class TestSqsTriggerPollingBehavior:
    """Tests for polling loop behavior - validates Issue #12 fix."""

    @pytest.mark.asyncio
    async def test_sequential_polling_no_overlap(self) -> None:
        """
        Test that polling is sequential (no overlapping polls).
        
        This validates the fix for Issue #12 - the timer-based polling
        was replaced with a sequential loop.
        """
        trigger = SqsTrigger(
            queue_url="https://sqs.us-east-1.amazonaws.com/123/queue",
            options=SqsTriggerOptions(polling_interval=timedelta(seconds=0)),
        )

        @trigger
        def handler(msg: SqsMessage) -> None:
            pass

        poll_count = 0
        max_concurrent_polls = 0
        current_concurrent_polls = 0

        async def mock_receive(*args, **kwargs) -> list[SqsMessage]:
            nonlocal poll_count, max_concurrent_polls, current_concurrent_polls
            current_concurrent_polls += 1
            max_concurrent_polls = max(max_concurrent_polls, current_concurrent_polls)
            poll_count += 1

            await asyncio.sleep(0.1)  # Simulate polling delay

            current_concurrent_polls -= 1

            # Stop after 3 polls
            if poll_count >= 3:
                trigger._running = False

            return []

        with patch("azure_functions_sqs.trigger.asyncio.to_thread", side_effect=mock_receive):
            with patch.object(trigger, "_get_client"):
                trigger._running = True
                await trigger._poll_loop_async()

        # Should never have more than 1 concurrent poll
        assert max_concurrent_polls == 1, "Polls should be sequential, not concurrent"
        assert poll_count == 3

    @pytest.mark.asyncio
    async def test_immediate_repoll_on_messages(self) -> None:
        """Test that trigger polls again immediately when messages are received."""
        trigger = SqsTrigger(
            queue_url="https://sqs.us-east-1.amazonaws.com/123/queue",
            options=SqsTriggerOptions(polling_interval=timedelta(seconds=10)),
        )

        @trigger
        def handler(msg: SqsMessage) -> None:
            pass

        poll_times: list[float] = []
        poll_count = 0

        async def mock_receive(*args, **kwargs) -> list[SqsMessage]:
            nonlocal poll_count
            poll_times.append(asyncio.get_event_loop().time())
            poll_count += 1

            if poll_count >= 3:
                trigger._running = False
                return []

            # Return a message to trigger immediate re-poll
            return [
                SqsMessage(
                    message_id=f"msg-{poll_count}",
                    receipt_handle=f"receipt-{poll_count}",
                    body="test",
                    md5_of_body="md5",
                )
            ]

        with patch("azure_functions_sqs.trigger.asyncio.to_thread", side_effect=mock_receive):
            with patch.object(trigger, "_get_client") as mock_get_client:
                mock_get_client.return_value.delete_message = MagicMock()
                trigger._running = True
                await trigger._poll_loop_async()

        # Check that polls happened quickly (no 10s delay between them)
        for i in range(1, len(poll_times)):
            time_diff = poll_times[i] - poll_times[i - 1]
            assert time_diff < 1.0, "Should re-poll immediately when messages received"
