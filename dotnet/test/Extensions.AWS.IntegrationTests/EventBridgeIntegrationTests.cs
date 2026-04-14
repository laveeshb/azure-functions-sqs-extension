using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using FluentAssertions;
using Xunit;

namespace Extensions.AWS.IntegrationTests;

[Collection("LocalStack")]
public class EventBridgeIntegrationTests
{
    private readonly LocalStackFixture _fixture;
    private readonly AmazonEventBridgeClient _eventBridgeClient;

    public EventBridgeIntegrationTests(LocalStackFixture fixture)
    {
        _fixture = fixture;
        _eventBridgeClient = new AmazonEventBridgeClient(
            _fixture.AccessKey,
            _fixture.SecretKey,
            new AmazonEventBridgeConfig
            {
                ServiceURL = _fixture.Endpoint,
                AuthenticationRegion = _fixture.Region
            });
    }

    [Fact]
    public async Task CreateEventBus_ShouldSucceed()
    {
        // Arrange
        var eventBusName = $"test-bus-{Guid.NewGuid():N}";

        // Act
        var response = await _eventBridgeClient.CreateEventBusAsync(new CreateEventBusRequest
        {
            Name = eventBusName
        });

        // Assert
        response.EventBusArn.Should().NotBeNullOrEmpty();
        response.EventBusArn.Should().Contain(eventBusName);
    }

    [Fact]
    public async Task ListEventBuses_ShouldIncludeDefault()
    {
        // Act
        var response = await _eventBridgeClient.ListEventBusesAsync(new ListEventBusesRequest());

        // Assert
        response.EventBuses.Should().NotBeEmpty();
        response.EventBuses.Should().Contain(b => b.Name == "default");
    }

    [Fact]
    public async Task PutEvents_ShouldSucceed()
    {
        // Arrange
        var eventBusName = $"test-bus-{Guid.NewGuid():N}";
        await _eventBridgeClient.CreateEventBusAsync(new CreateEventBusRequest
        {
            Name = eventBusName
        });

        var events = new List<PutEventsRequestEntry>
        {
            new PutEventsRequestEntry
            {
                EventBusName = eventBusName,
                Source = "integration.test",
                DetailType = "TestEvent",
                Detail = "{\"message\": \"Hello from integration test\"}"
            }
        };

        // Act
        var response = await _eventBridgeClient.PutEventsAsync(new PutEventsRequest
        {
            Entries = events
        });

        // Assert
        response.FailedEntryCount.Should().Be(0);
        response.Entries.Should().HaveCount(1);
        response.Entries[0].EventId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task PutMultipleEvents_ShouldSucceed()
    {
        // Arrange
        var eventBusName = $"test-bus-{Guid.NewGuid():N}";
        await _eventBridgeClient.CreateEventBusAsync(new CreateEventBusRequest
        {
            Name = eventBusName
        });

        var events = Enumerable.Range(1, 5).Select(i => new PutEventsRequestEntry
        {
            EventBusName = eventBusName,
            Source = "integration.test",
            DetailType = "BatchEvent",
            Detail = $"{{\"index\": {i}}}"
        }).ToList();

        // Act
        var response = await _eventBridgeClient.PutEventsAsync(new PutEventsRequest
        {
            Entries = events
        });

        // Assert
        response.FailedEntryCount.Should().Be(0);
        response.Entries.Should().HaveCount(5);
    }

    [Fact]
    public async Task CreateRule_ShouldSucceed()
    {
        // Arrange
        var eventBusName = $"test-bus-{Guid.NewGuid():N}";
        var ruleName = $"test-rule-{Guid.NewGuid():N}";
        
        await _eventBridgeClient.CreateEventBusAsync(new CreateEventBusRequest
        {
            Name = eventBusName
        });

        // Act
        var response = await _eventBridgeClient.PutRuleAsync(new PutRuleRequest
        {
            Name = ruleName,
            EventBusName = eventBusName,
            EventPattern = "{\"source\": [\"integration.test\"]}",
            State = RuleState.ENABLED
        });

        // Assert
        response.RuleArn.Should().NotBeNullOrEmpty();
        response.RuleArn.Should().Contain(ruleName);
    }

    [Fact]
    public async Task ListRules_ShouldReturnCreatedRule()
    {
        // Arrange
        var eventBusName = $"test-bus-{Guid.NewGuid():N}";
        var ruleName = $"test-rule-{Guid.NewGuid():N}";
        
        await _eventBridgeClient.CreateEventBusAsync(new CreateEventBusRequest
        {
            Name = eventBusName
        });

        await _eventBridgeClient.PutRuleAsync(new PutRuleRequest
        {
            Name = ruleName,
            EventBusName = eventBusName,
            EventPattern = "{\"source\": [\"integration.test\"]}",
            State = RuleState.ENABLED
        });

        // Act
        var response = await _eventBridgeClient.ListRulesAsync(new ListRulesRequest
        {
            EventBusName = eventBusName
        });

        // Assert
        response.Rules.Should().Contain(r => r.Name == ruleName);
    }

    [Fact]
    public async Task DescribeEventBus_ShouldReturnDetails()
    {
        // Arrange
        var eventBusName = $"test-bus-{Guid.NewGuid():N}";
        await _eventBridgeClient.CreateEventBusAsync(new CreateEventBusRequest
        {
            Name = eventBusName
        });

        // Act
        var response = await _eventBridgeClient.DescribeEventBusAsync(new DescribeEventBusRequest
        {
            Name = eventBusName
        });

        // Assert
        response.Name.Should().Be(eventBusName);
        response.Arn.Should().Contain(eventBusName);
    }

    [Fact]
    public async Task DeleteEventBus_ShouldSucceed()
    {
        // Arrange
        var eventBusName = $"test-bus-{Guid.NewGuid():N}";
        await _eventBridgeClient.CreateEventBusAsync(new CreateEventBusRequest
        {
            Name = eventBusName
        });

        // Act
        await _eventBridgeClient.DeleteEventBusAsync(new DeleteEventBusRequest
        {
            Name = eventBusName
        });

        // Assert - Should throw when trying to describe deleted bus
        var act = async () => await _eventBridgeClient.DescribeEventBusAsync(new DescribeEventBusRequest
        {
            Name = eventBusName
        });

        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }

    [Fact]
    public async Task PutEvents_WithResources_ShouldSucceed()
    {
        // Arrange
        var eventBusName = $"test-bus-{Guid.NewGuid():N}";
        await _eventBridgeClient.CreateEventBusAsync(new CreateEventBusRequest
        {
            Name = eventBusName
        });

        var events = new List<PutEventsRequestEntry>
        {
            new PutEventsRequestEntry
            {
                EventBusName = eventBusName,
                Source = "integration.test",
                DetailType = "ResourceEvent",
                Detail = "{\"action\": \"created\"}",
                Resources = new List<string> { "arn:aws:s3:::my-bucket", "arn:aws:sqs:us-east-1:123456789:my-queue" }
            }
        };

        // Act
        var response = await _eventBridgeClient.PutEventsAsync(new PutEventsRequest
        {
            Entries = events
        });

        // Assert
        response.FailedEntryCount.Should().Be(0);
        response.Entries.Should().HaveCount(1);
    }
}
