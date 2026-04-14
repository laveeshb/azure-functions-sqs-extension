using Azure.WebJobs.Extensions.EventBridge;
using Xunit;
using FluentAssertions;
using System.Text.Json;

namespace Extensions.AWS.UnitTests;

public class EventBridgeTriggerTests
{
    #region EventBridgeTriggerAttribute Tests

    [Fact]
    public void EventBridgeTriggerAttribute_DefaultValues()
    {
        var attribute = new EventBridgeTriggerAttribute();

        attribute.Route.Should().BeNull();
        attribute.Source.Should().BeNull();
        attribute.DetailType.Should().BeNull();
    }

    [Fact]
    public void EventBridgeTriggerAttribute_SetsAllProperties()
    {
        var attribute = new EventBridgeTriggerAttribute
        {
            Route = "webhooks/events",
            Source = "my-application",
            DetailType = "order.created"
        };

        attribute.Route.Should().Be("webhooks/events");
        attribute.Source.Should().Be("my-application");
        attribute.DetailType.Should().Be("order.created");
    }

    [Fact]
    public void EventBridgeTriggerAttribute_Constructor_SetsRoute()
    {
        var attribute = new EventBridgeTriggerAttribute("custom-route");

        attribute.Route.Should().Be("custom-route");
    }

    #endregion

    #region EventBridgeEvent Tests

    [Fact]
    public void EventBridgeEvent_DeserializesCorrectly()
    {
        var json = @"{
            ""version"": ""0"",
            ""id"": ""event-123"",
            ""detail-type"": ""OrderCreated"",
            ""source"": ""my-app"",
            ""account"": ""123456789012"",
            ""time"": ""2024-01-15T12:00:00Z"",
            ""region"": ""us-east-1"",
            ""resources"": [""arn:aws:ec2:us-east-1:123456789012:instance/i-1234567890abcdef0""],
            ""detail"": {""orderId"": ""12345"", ""amount"": 99.99}
        }";

        var evt = JsonSerializer.Deserialize<EventBridgeEvent>(json);

        evt.Should().NotBeNull();
        evt!.Id.Should().Be("event-123");
        evt.DetailType.Should().Be("OrderCreated");
        evt.Source.Should().Be("my-app");
        evt.Account.Should().Be("123456789012");
        evt.Region.Should().Be("us-east-1");
        evt.Resources.Should().HaveCount(1);
        evt.Detail.Should().NotBeNull();
    }

    [Fact]
    public void EventBridgeEvent_GetDetail_DeserializesTypedPayload()
    {
        var json = @"{
            ""id"": ""event-123"",
            ""source"": ""my-app"",
            ""detail-type"": ""OrderCreated"",
            ""detail"": {""orderId"": ""12345"", ""amount"": 99.99}
        }";

        var evt = JsonSerializer.Deserialize<EventBridgeEvent>(json);

        var detail = evt!.GetDetail<TestOrderDetail>();

        detail.Should().NotBeNull();
        detail!.OrderId.Should().Be("12345");
        detail.Amount.Should().Be(99.99m);
    }

    [Fact]
    public void EventBridgeEvent_GetDetail_WithNullDetail_ReturnsNull()
    {
        var evt = new EventBridgeEvent
        {
            Id = "event-123",
            Source = "my-app",
            Detail = null
        };

        var detail = evt.GetDetail<TestOrderDetail>();

        detail.Should().BeNull();
    }

    [Fact]
    public void EventBridgeEvent_GetDetailRaw_ReturnsJsonString()
    {
        var json = @"{
            ""id"": ""event-123"",
            ""source"": ""my-app"",
            ""detail"": {""orderId"": ""12345""}
        }";

        var evt = JsonSerializer.Deserialize<EventBridgeEvent>(json);

        var raw = evt!.GetDetailRaw();

        raw.Should().Contain("orderId");
        raw.Should().Contain("12345");
    }

    #endregion

    #region EventBridgeEvent Serialization Tests

    [Fact]
    public void EventBridgeEvent_SerializesCorrectly()
    {
        var json = @"{
            ""id"": ""event-123"",
            ""source"": ""my-app"",
            ""detail-type"": ""OrderCreated"",
            ""account"": ""123456789012"",
            ""region"": ""us-east-1"",
            ""time"": ""2024-01-15T12:00:00Z"",
            ""detail"": {""orderId"": ""12345""}
        }";

        var evt = JsonSerializer.Deserialize<EventBridgeEvent>(json);
        var serialized = JsonSerializer.Serialize(evt);
        var deserialized = JsonSerializer.Deserialize<EventBridgeEvent>(serialized);

        deserialized.Should().NotBeNull();
        deserialized!.Id.Should().Be("event-123");
        deserialized.Source.Should().Be("my-app");
    }

    #endregion

    private class TestOrderDetail
    {
        [System.Text.Json.Serialization.JsonPropertyName("orderId")]
        public string? OrderId { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("amount")]
        public decimal Amount { get; set; }
    }
}
