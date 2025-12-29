"""SQS Message model - matches .NET Amazon.SQS.Model.Message contract."""

from __future__ import annotations

import base64
from dataclasses import dataclass, field
from typing import Any


@dataclass
class MessageAttributeValue:
    """SQS Message attribute value - matches .NET MessageAttributeValue."""

    data_type: str
    """The data type of the message attribute (String, Number, Binary)."""

    string_value: str | None = None
    """String value when data_type is String or Number."""

    binary_value: bytes | None = None
    """Binary value when data_type is Binary."""

    @classmethod
    def from_boto3(cls, attr: dict[str, Any]) -> MessageAttributeValue:
        """Create from boto3 message attribute dict."""
        return cls(
            data_type=attr.get("DataType", "String"),
            string_value=attr.get("StringValue"),
            binary_value=attr.get("BinaryValue"),
        )

    def to_dict(self) -> dict[str, Any]:
        """Convert to dictionary for JSON serialization."""
        result: dict[str, Any] = {"DataType": self.data_type}
        if self.string_value is not None:
            result["StringValue"] = self.string_value
        if self.binary_value is not None:
            # Use base64 encoding for binary data to ensure safe JSON serialization
            result["BinaryValue"] = base64.b64encode(self.binary_value).decode("ascii")
        return result


@dataclass
class SqsMessage:
    """
    SQS Message - matches .NET Amazon.SQS.Model.Message contract.

    This class mirrors the structure of the .NET SDK's Message class
    to ensure consistent behavior across language implementations.
    """

    message_id: str
    """Unique identifier for the message."""

    receipt_handle: str
    """Handle used for deleting or changing message visibility."""

    body: str
    """The message body."""

    md5_of_body: str
    """MD5 digest of the message body."""

    attributes: dict[str, str] = field(default_factory=dict)
    """
    System attributes of the message.

    Common attributes:
    - SentTimestamp: When the message was sent (epoch milliseconds)
    - ApproximateReceiveCount: Number of times the message has been received
    - ApproximateFirstReceiveTimestamp: When first received (epoch milliseconds)
    - SenderId: AWS account ID of the sender
    - MessageGroupId: (FIFO queues) Message group identifier
    - MessageDeduplicationId: (FIFO queues) Deduplication token
    - SequenceNumber: (FIFO queues) Large, non-consecutive number
    """

    message_attributes: dict[str, MessageAttributeValue] = field(default_factory=dict)
    """Custom attributes set by the message sender."""

    @classmethod
    def from_boto3(cls, msg: dict[str, Any]) -> SqsMessage:
        """
        Create SqsMessage from boto3 receive_message response.

        Args:
            msg: A message dict from boto3 SQS receive_message response.

        Returns:
            SqsMessage instance with all fields populated.
        """
        message_attributes: dict[str, MessageAttributeValue] = {}
        for name, attr in msg.get("MessageAttributes", {}).items():
            message_attributes[name] = MessageAttributeValue.from_boto3(attr)

        return cls(
            message_id=msg.get("MessageId", ""),
            receipt_handle=msg.get("ReceiptHandle", ""),
            body=msg.get("Body", ""),
            md5_of_body=msg.get("MD5OfBody", ""),
            attributes=msg.get("Attributes", {}),
            message_attributes=message_attributes,
        )

    def to_dict(self) -> dict[str, Any]:
        """
        Convert to dictionary for JSON serialization.

        Returns:
            Dictionary matching the .NET Message JSON structure.
        """
        return {
            "MessageId": self.message_id,
            "ReceiptHandle": self.receipt_handle,
            "Body": self.body,
            "MD5OfBody": self.md5_of_body,
            "Attributes": self.attributes,
            "MessageAttributes": {
                name: attr.to_dict() for name, attr in self.message_attributes.items()
            },
        }

    @property
    def sent_timestamp(self) -> int | None:
        """Get the SentTimestamp attribute as integer (epoch milliseconds)."""
        ts = self.attributes.get("SentTimestamp")
        return int(ts) if ts else None

    @property
    def approximate_receive_count(self) -> int:
        """Get the ApproximateReceiveCount attribute."""
        count = self.attributes.get("ApproximateReceiveCount", "0")
        return int(count)

    @property
    def sender_id(self) -> str | None:
        """Get the SenderId attribute (AWS account ID of sender)."""
        return self.attributes.get("SenderId")

    # FIFO queue attributes
    @property
    def message_group_id(self) -> str | None:
        """Get the MessageGroupId (FIFO queues only)."""
        return self.attributes.get("MessageGroupId")

    @property
    def message_deduplication_id(self) -> str | None:
        """Get the MessageDeduplicationId (FIFO queues only)."""
        return self.attributes.get("MessageDeduplicationId")

    @property
    def sequence_number(self) -> str | None:
        """Get the SequenceNumber (FIFO queues only)."""
        return self.attributes.get("SequenceNumber")
