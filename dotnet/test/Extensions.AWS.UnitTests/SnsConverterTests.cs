using System.Text.Json;
using Azure.Functions.Worker.Extensions.SNS;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker.Converters;
using Moq;
using Xunit;

namespace Extensions.AWS.UnitTests;

public class SnsConverterTests
{
    private readonly SnsMessageConverter _converter = new();

    #region SnsNotification Tests

    [Fact]
    public async Task ConvertAsync_WithValidSnsJson_ReturnsSnsNotification()
    {
        // Arrange
        var json = """
        {
            "Type": "Notification",
            "MessageId": "msg-12345678-1234-1234-1234-123456789012",
            "TopicArn": "arn:aws:sns:us-east-1:123456789012:my-topic",
            "Subject": "Test Subject",
            "Message": "{\"orderId\":\"ABC123\"}",
            "Timestamp": "2024-01-15T10:30:00Z",
            "SignatureVersion": "1",
            "Signature": "abc123signature",
            "SigningCertURL": "https://sns.us-east-1.amazonaws.com/cert.pem",
            "UnsubscribeURL": "https://sns.us-east-1.amazonaws.com/unsubscribe"
        }
        """;

        var context = CreateConverterContext(json, typeof(SnsNotification));

        // Act
        var result = await _converter.ConvertAsync(context);

        // Assert
        result.Status.Should().Be(ConversionStatus.Succeeded);
        result.Value.Should().BeOfType<SnsNotification>();
        var notification = (SnsNotification)result.Value!;
        notification.Type.Should().Be("Notification");
        notification.MessageId.Should().Be("msg-12345678-1234-1234-1234-123456789012");
        notification.TopicArn.Should().Be("arn:aws:sns:us-east-1:123456789012:my-topic");
        notification.Subject.Should().Be("Test Subject");
        notification.Message.Should().Contain("orderId");
    }

    [Fact]
    public async Task ConvertAsync_WithSqsWrappedSnsJson_ReturnsSnsNotification()
    {
        // Arrange - SNS notifications come wrapped in SQS message
        var snsJson = """
        {
            "Type": "Notification",
            "MessageId": "sns-msg-123",
            "TopicArn": "arn:aws:sns:us-east-1:123456789012:orders",
            "Message": "Hello World"
        }
        """;
        var sqsWrapper = new { Body = snsJson };
        var json = JsonSerializer.Serialize(sqsWrapper);

        var context = CreateConverterContext(json, typeof(SnsNotification));

        // Act
        var result = await _converter.ConvertAsync(context);

        // Assert
        result.Status.Should().Be(ConversionStatus.Succeeded);
        var notification = (SnsNotification)result.Value!;
        notification.MessageId.Should().Be("sns-msg-123");
        notification.Message.Should().Be("Hello World");
    }

    [Fact]
    public async Task ConvertAsync_WithStringTargetType_ReturnsMessageString()
    {
        // Arrange
        var json = """
        {
            "Type": "Notification",
            "MessageId": "msg-123",
            "Message": "This is the message content"
        }
        """;

        var context = CreateConverterContext(json, typeof(string));

        // Act
        var result = await _converter.ConvertAsync(context);

        // Assert
        result.Status.Should().Be(ConversionStatus.Succeeded);
        result.Value.Should().Be("This is the message content");
    }

    [Fact]
    public async Task ConvertAsync_WithEmptySource_ReturnsUnhandled()
    {
        // Arrange
        var context = CreateConverterContext("", typeof(SnsNotification));

        // Act
        var result = await _converter.ConvertAsync(context);

        // Assert
        result.Status.Should().Be(ConversionStatus.Unhandled);
    }

    [Fact]
    public async Task ConvertAsync_WithInvalidJson_ReturnsFailed()
    {
        // Arrange
        var context = CreateConverterContext("not valid json", typeof(SnsNotification));

        // Act
        var result = await _converter.ConvertAsync(context);

        // Assert
        result.Status.Should().Be(ConversionStatus.Failed);
    }

    #endregion

    #region SnsNotification Model Tests

    [Fact]
    public void SnsNotification_Deserializes_AllProperties()
    {
        // Arrange
        var json = """
        {
            "Type": "Notification",
            "MessageId": "test-msg-id",
            "TopicArn": "arn:aws:sns:eu-west-1:111122223333:test-topic",
            "Subject": "Important Update",
            "Message": "The content of the message",
            "Timestamp": "2024-06-15T14:30:00Z",
            "SignatureVersion": "1",
            "Signature": "sig123",
            "SigningCertURL": "https://example.com/cert",
            "UnsubscribeURL": "https://example.com/unsub",
            "MessageAttributes": {
                "attr1": {
                    "Type": "String",
                    "Value": "value1"
                }
            }
        }
        """;

        // Act
        var notification = JsonSerializer.Deserialize<SnsNotification>(json);

        // Assert
        notification.Should().NotBeNull();
        notification!.Type.Should().Be("Notification");
        notification.MessageId.Should().Be("test-msg-id");
        notification.TopicArn.Should().Contain("test-topic");
        notification.Subject.Should().Be("Important Update");
        notification.Message.Should().Be("The content of the message");
        notification.Timestamp.Should().Be(new DateTime(2024, 6, 15, 14, 30, 0, DateTimeKind.Utc));
        notification.MessageAttributes.Should().ContainKey("attr1");
    }

    [Fact]
    public void SnsNotification_WithMessageAttributes_DeserializesCorrectly()
    {
        // Arrange
        var json = """
        {
            "Type": "Notification",
            "MessageId": "msg-with-attrs",
            "Message": "Hello",
            "MessageAttributes": {
                "customerId": {
                    "Type": "String",
                    "Value": "cust-123"
                },
                "priority": {
                    "Type": "Number",
                    "Value": "1"
                }
            }
        }
        """;

        // Act
        var notification = JsonSerializer.Deserialize<SnsNotification>(json);

        // Assert
        notification!.MessageAttributes.Should().HaveCount(2);
        notification.MessageAttributes!["customerId"].Value.Should().Be("cust-123");
        notification.MessageAttributes["priority"].Value.Should().Be("1");
    }

    #endregion

    #region SnsOutputMessage Model Tests

    [Fact]
    public void SnsOutputMessage_Serializes_Correctly()
    {
        // Arrange
        var outputMessage = new SnsOutputMessage
        {
            Message = "Test message content",
            Subject = "Test Subject",
            MessageGroupId = "group-1",
            MessageDeduplicationId = "dedup-1",
            MessageAttributes = new Dictionary<string, SnsMessageAttributeValue>
            {
                ["attr1"] = new SnsMessageAttributeValue { DataType = "String", StringValue = "val1" }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(outputMessage);
        var deserialized = JsonSerializer.Deserialize<SnsOutputMessage>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Message!.ToString().Should().Be("Test message content");
        deserialized.Subject.Should().Be("Test Subject");
        deserialized.MessageGroupId.Should().Be("group-1");
    }

    #endregion

    private static ConverterContext CreateConverterContext(string? source, Type targetType)
    {
        var functionContext = new Mock<Microsoft.Azure.Functions.Worker.FunctionContext>();
        return new TestConverterContext(targetType, source, functionContext.Object);
    }
}
