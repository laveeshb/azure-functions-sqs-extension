using System.Text.Json;
using Azure.Functions.Worker.Extensions.EventBridge;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker.Converters;
using Moq;
using Xunit;

namespace Extensions.AWS.UnitTests;

public class EventBridgeConverterTests
{
    private readonly EventBridgeMessageConverter _converter = new();

    #region EventBridgeEvent Tests

    [Fact]
    public async Task ConvertAsync_WithValidEventBridgeJson_ReturnsEventBridgeEvent()
    {
        // Arrange
        var json = """
        {
            "version": "0",
            "id": "12345678-1234-1234-1234-123456789012",
            "source": "custom.myapp",
            "account": "123456789012",
            "region": "us-east-1",
            "time": "2024-01-15T10:30:00Z",
            "detail-type": "OrderCreated",
            "detail": "{\"orderId\":\"ABC123\",\"amount\":99.99}",
            "resources": ["arn:aws:events:us-east-1:123456789012:event-bus/my-bus"]
        }
        """;

        var context = CreateConverterContext(json, typeof(EventBridgeEvent));

        // Act
        var result = await _converter.ConvertAsync(context);

        // Assert
        result.Status.Should().Be(ConversionStatus.Succeeded);
        result.Value.Should().BeOfType<EventBridgeEvent>();
        var evt = (EventBridgeEvent)result.Value!;
        evt.Version.Should().Be("0");
        evt.Id.Should().Be("12345678-1234-1234-1234-123456789012");
        evt.Source.Should().Be("custom.myapp");
        evt.Account.Should().Be("123456789012");
        evt.Region.Should().Be("us-east-1");
        evt.DetailType.Should().Be("OrderCreated");
        evt.Detail.Should().Contain("orderId");
        evt.Resources.Should().ContainSingle();
    }

    [Fact]
    public async Task ConvertAsync_WithSqsWrappedEventBridgeJson_ReturnsEventBridgeEvent()
    {
        // Arrange - SQS wraps EventBridge events in a Body property
        var eventBridgeJson = """
        {
            "version": "0",
            "id": "event-123",
            "source": "aws.ec2",
            "detail-type": "EC2 Instance State-change Notification",
            "detail": "{\"state\":\"running\"}"
        }
        """;
        var sqsWrapper = new { Body = eventBridgeJson };
        var json = JsonSerializer.Serialize(sqsWrapper);

        var context = CreateConverterContext(json, typeof(EventBridgeEvent));

        // Act
        var result = await _converter.ConvertAsync(context);

        // Assert
        result.Status.Should().Be(ConversionStatus.Succeeded);
        var evt = (EventBridgeEvent)result.Value!;
        evt.Id.Should().Be("event-123");
        evt.Source.Should().Be("aws.ec2");
    }

    [Fact]
    public async Task ConvertAsync_WithStringTargetType_ReturnsDetailString()
    {
        // Arrange
        var json = """
        {
            "version": "0",
            "id": "event-123",
            "source": "custom.myapp",
            "detail-type": "Test",
            "detail": "{\"key\":\"value\"}"
        }
        """;

        var context = CreateConverterContext(json, typeof(string));

        // Act
        var result = await _converter.ConvertAsync(context);

        // Assert
        result.Status.Should().Be(ConversionStatus.Succeeded);
        result.Value.Should().BeOfType<string>();
        var detail = (string)result.Value!;
        detail.Should().Contain("key");
        detail.Should().Contain("value");
    }

    [Fact]
    public async Task ConvertAsync_WithEmptySource_ReturnsUnhandled()
    {
        // Arrange
        var context = CreateConverterContext("", typeof(EventBridgeEvent));

        // Act
        var result = await _converter.ConvertAsync(context);

        // Assert
        result.Status.Should().Be(ConversionStatus.Unhandled);
    }

    [Fact]
    public async Task ConvertAsync_WithNullSource_ReturnsUnhandled()
    {
        // Arrange
        var context = CreateConverterContext(null, typeof(EventBridgeEvent));

        // Act
        var result = await _converter.ConvertAsync(context);

        // Assert
        result.Status.Should().Be(ConversionStatus.Unhandled);
    }

    [Fact]
    public async Task ConvertAsync_WithInvalidJson_ReturnsFailed()
    {
        // Arrange
        var context = CreateConverterContext("not valid json {{{", typeof(EventBridgeEvent));

        // Act
        var result = await _converter.ConvertAsync(context);

        // Assert
        result.Status.Should().Be(ConversionStatus.Failed);
        result.Error.Should().NotBeNull();
    }

    #endregion

    #region EventBridgeEvent Model Tests

    [Fact]
    public void EventBridgeEvent_Deserializes_AllProperties()
    {
        // Arrange
        var json = """
        {
            "version": "0",
            "id": "test-id",
            "source": "test-source",
            "account": "111122223333",
            "region": "eu-west-1",
            "time": "2024-06-15T14:30:00Z",
            "detail-type": "TestType",
            "detail": "{\"foo\":\"bar\"}",
            "resources": ["arn:1", "arn:2"]
        }
        """;

        // Act
        var evt = JsonSerializer.Deserialize<EventBridgeEvent>(json);

        // Assert
        evt.Should().NotBeNull();
        evt!.Version.Should().Be("0");
        evt.Id.Should().Be("test-id");
        evt.Source.Should().Be("test-source");
        evt.Account.Should().Be("111122223333");
        evt.Region.Should().Be("eu-west-1");
        evt.Time.Should().Be(new DateTime(2024, 6, 15, 14, 30, 0, DateTimeKind.Utc));
        evt.DetailType.Should().Be("TestType");
        evt.Detail.Should().Be("{\"foo\":\"bar\"}");
        evt.Resources.Should().HaveCount(2);
    }

    #endregion

    #region EventBridgeOutputEvent Model Tests

    [Fact]
    public void EventBridgeOutputEvent_Serializes_Correctly()
    {
        // Arrange
        var outputEvent = new EventBridgeOutputEvent
        {
            Source = "my.application",
            DetailType = "OrderCreated",
            Detail = "{\"orderId\":\"123\"}",
            EventBusName = "my-bus",
            Resources = new List<string> { "arn:aws:resource" }
        };

        // Act
        var json = JsonSerializer.Serialize(outputEvent);
        var deserialized = JsonSerializer.Deserialize<EventBridgeOutputEvent>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Source.Should().Be("my.application");
        deserialized.DetailType.Should().Be("OrderCreated");
        deserialized.Detail!.ToString().Should().Contain("orderId");
        deserialized.EventBusName.Should().Be("my-bus");
    }

    #endregion

    private static ConverterContext CreateConverterContext(string? source, Type targetType)
    {
        var functionContext = new Mock<Microsoft.Azure.Functions.Worker.FunctionContext>();
        return new TestConverterContext(targetType, source, functionContext.Object);
    }
}

// Helper class since ConverterContext is abstract
internal class TestConverterContext : ConverterContext
{
    public TestConverterContext(Type targetType, object? source, Microsoft.Azure.Functions.Worker.FunctionContext functionContext)
    {
        TargetType = targetType;
        Source = source;
        FunctionContext = functionContext;
        Properties = new Dictionary<string, object>();
    }

    public override Type TargetType { get; }
    public override object? Source { get; }
    public override Microsoft.Azure.Functions.Worker.FunctionContext FunctionContext { get; }
    public override IReadOnlyDictionary<string, object> Properties { get; }
}
