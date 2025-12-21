"""Tests for SqsMessage model."""

import pytest

from azure_functions_sqs.message import MessageAttributeValue, SqsMessage


class TestMessageAttributeValue:
    """Tests for MessageAttributeValue."""

    def test_from_boto3_string_value(self) -> None:
        """Test parsing string attribute from boto3."""
        attr = {
            "DataType": "String",
            "StringValue": "test-value",
        }
        result = MessageAttributeValue.from_boto3(attr)

        assert result.data_type == "String"
        assert result.string_value == "test-value"
        assert result.binary_value is None

    def test_from_boto3_number_value(self) -> None:
        """Test parsing number attribute from boto3."""
        attr = {
            "DataType": "Number",
            "StringValue": "42",
        }
        result = MessageAttributeValue.from_boto3(attr)

        assert result.data_type == "Number"
        assert result.string_value == "42"

    def test_to_dict(self) -> None:
        """Test converting to dictionary."""
        attr = MessageAttributeValue(
            data_type="String",
            string_value="test",
        )
        result = attr.to_dict()

        assert result == {"DataType": "String", "StringValue": "test"}


class TestSqsMessage:
    """Tests for SqsMessage."""

    def test_from_boto3_basic(self) -> None:
        """Test parsing basic message from boto3."""
        msg = {
            "MessageId": "msg-123",
            "ReceiptHandle": "receipt-abc",
            "Body": "Hello, World!",
            "MD5OfBody": "md5-hash",
            "Attributes": {
                "SentTimestamp": "1234567890000",
                "ApproximateReceiveCount": "1",
            },
            "MessageAttributes": {},
        }

        result = SqsMessage.from_boto3(msg)

        assert result.message_id == "msg-123"
        assert result.receipt_handle == "receipt-abc"
        assert result.body == "Hello, World!"
        assert result.md5_of_body == "md5-hash"
        assert result.sent_timestamp == 1234567890000
        assert result.approximate_receive_count == 1

    def test_from_boto3_with_message_attributes(self) -> None:
        """Test parsing message with custom attributes."""
        msg = {
            "MessageId": "msg-456",
            "ReceiptHandle": "receipt-def",
            "Body": "Test body",
            "MD5OfBody": "md5",
            "Attributes": {},
            "MessageAttributes": {
                "CustomAttr": {
                    "DataType": "String",
                    "StringValue": "custom-value",
                }
            },
        }

        result = SqsMessage.from_boto3(msg)

        assert "CustomAttr" in result.message_attributes
        assert result.message_attributes["CustomAttr"].string_value == "custom-value"

    def test_from_boto3_fifo_queue(self) -> None:
        """Test parsing FIFO queue message."""
        msg = {
            "MessageId": "msg-789",
            "ReceiptHandle": "receipt-ghi",
            "Body": "FIFO message",
            "MD5OfBody": "md5",
            "Attributes": {
                "MessageGroupId": "group-1",
                "MessageDeduplicationId": "dedup-1",
                "SequenceNumber": "12345",
            },
            "MessageAttributes": {},
        }

        result = SqsMessage.from_boto3(msg)

        assert result.message_group_id == "group-1"
        assert result.message_deduplication_id == "dedup-1"
        assert result.sequence_number == "12345"

    def test_to_dict(self) -> None:
        """Test converting message to dictionary (JSON-serializable)."""
        message = SqsMessage(
            message_id="msg-123",
            receipt_handle="receipt-abc",
            body="Test body",
            md5_of_body="md5",
            attributes={"SentTimestamp": "1234567890"},
            message_attributes={
                "Attr1": MessageAttributeValue(data_type="String", string_value="val1")
            },
        )

        result = message.to_dict()

        assert result["MessageId"] == "msg-123"
        assert result["Body"] == "Test body"
        assert result["Attributes"]["SentTimestamp"] == "1234567890"
        assert result["MessageAttributes"]["Attr1"]["StringValue"] == "val1"

    def test_to_dict_matches_dotnet_contract(self) -> None:
        """Verify to_dict output matches .NET JSON serialization contract."""
        message = SqsMessage(
            message_id="test-id",
            receipt_handle="test-handle",
            body="test body",
            md5_of_body="abc123",
            attributes={"ApproximateReceiveCount": "2"},
            message_attributes={},
        )

        result = message.to_dict()

        # These keys must match .NET Amazon.SQS.Model.Message property names
        assert "MessageId" in result  # Not message_id
        assert "ReceiptHandle" in result  # Not receipt_handle
        assert "Body" in result
        assert "MD5OfBody" in result  # Not md5_of_body
        assert "Attributes" in result
        assert "MessageAttributes" in result
