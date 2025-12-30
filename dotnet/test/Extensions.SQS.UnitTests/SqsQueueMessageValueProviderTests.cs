namespace Extensions.SQS.UnitTests;

using System;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using Azure.WebJobs.Extensions.SQS;
using FluentAssertions;
using Xunit;

public class SqsQueueMessageValueProviderTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidMessage_CreatesProvider()
    {
        // Arrange
        var message = new Message
        {
            MessageId = "test-id",
            Body = "test body"
        };

        // Act
        var provider = new SqsQueueMessageValueProvider(message);

        // Assert
        provider.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullValue_ThrowsArgumentNullException()
    {
        // Act & Assert
        var action = () => new SqsQueueMessageValueProvider(null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("value");
    }

    [Fact]
    public void Constructor_WithStringValue_CreatesProvider()
    {
        // Arrange
        var value = "test string body";

        // Act
        var provider = new SqsQueueMessageValueProvider(value);

        // Assert
        provider.Should().NotBeNull();
    }

    #endregion

    #region Type Property Tests

    [Fact]
    public void Type_ReturnsMessageType()
    {
        // Arrange
        var message = new Message { Body = "test" };
        var provider = new SqsQueueMessageValueProvider(message);

        // Act
        var type = provider.Type;

        // Assert
        type.Should().Be(typeof(Message));
    }

    [Fact]
    public void Type_WithStringValue_StillReturnsMessageType()
    {
        // Arrange
        var provider = new SqsQueueMessageValueProvider("test string");

        // Act
        var type = provider.Type;

        // Assert
        // Note: This is the current behavior - always returns Message type
        // regardless of actual value type
        type.Should().Be(typeof(Message));
    }

    #endregion

    #region GetValueAsync Tests

    [Fact]
    public async Task GetValueAsync_WithMessage_ReturnsMessage()
    {
        // Arrange
        var message = new Message
        {
            MessageId = "test-id",
            Body = "test body",
            ReceiptHandle = "receipt-123"
        };
        var provider = new SqsQueueMessageValueProvider(message);

        // Act
        var result = await provider.GetValueAsync();

        // Assert
        result.Should().BeSameAs(message);
    }

    [Fact]
    public async Task GetValueAsync_WithString_ReturnsString()
    {
        // Arrange
        var value = "test string body";
        var provider = new SqsQueueMessageValueProvider(value);

        // Act
        var result = await provider.GetValueAsync();

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public async Task GetValueAsync_CalledMultipleTimes_ReturnsSameValue()
    {
        // Arrange
        var message = new Message { Body = "test" };
        var provider = new SqsQueueMessageValueProvider(message);

        // Act
        var result1 = await provider.GetValueAsync();
        var result2 = await provider.GetValueAsync();
        var result3 = await provider.GetValueAsync();

        // Assert
        result1.Should().BeSameAs(result2);
        result2.Should().BeSameAs(result3);
    }

    #endregion

    #region ToInvokeString Tests

    [Fact]
    public void ToInvokeString_WithMessage_ReturnsToStringResult()
    {
        // Arrange
        var message = new Message
        {
            MessageId = "test-id",
            Body = "test body"
        };
        var provider = new SqsQueueMessageValueProvider(message);

        // Act
        var result = provider.ToInvokeString();

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Be(message.ToString());
    }

    [Fact]
    public void ToInvokeString_WithString_ReturnsString()
    {
        // Arrange
        var value = "test string body";
        var provider = new SqsQueueMessageValueProvider(value);

        // Act
        var result = provider.ToInvokeString();

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public void ToInvokeString_WithObjectThatReturnsNull_ReturnsEmptyString()
    {
        // Arrange
        var mockObject = new ObjectWithNullToString();
        var provider = new SqsQueueMessageValueProvider(mockObject);

        // Act
        var result = provider.ToInvokeString();

        // Assert
        result.Should().Be(string.Empty);
    }

    private class ObjectWithNullToString
    {
        public override string? ToString() => null;
    }

    #endregion
}
