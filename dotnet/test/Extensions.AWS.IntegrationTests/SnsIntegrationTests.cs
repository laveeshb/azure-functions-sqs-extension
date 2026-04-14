using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using FluentAssertions;
using Xunit;

namespace Extensions.AWS.IntegrationTests;

[Collection("LocalStack")]
public class SnsIntegrationTests
{
    private readonly LocalStackFixture _fixture;
    private readonly AmazonSimpleNotificationServiceClient _snsClient;

    public SnsIntegrationTests(LocalStackFixture fixture)
    {
        _fixture = fixture;
        _snsClient = new AmazonSimpleNotificationServiceClient(
            _fixture.AccessKey,
            _fixture.SecretKey,
            new AmazonSimpleNotificationServiceConfig
            {
                ServiceURL = _fixture.Endpoint,
                AuthenticationRegion = _fixture.Region
            });
    }

    [Fact]
    public async Task CreateTopic_ShouldSucceed()
    {
        // Arrange
        var topicName = $"test-topic-{Guid.NewGuid():N}";

        // Act
        var response = await _snsClient.CreateTopicAsync(new CreateTopicRequest
        {
            Name = topicName
        });

        // Assert
        response.TopicArn.Should().NotBeNullOrEmpty();
        response.TopicArn.Should().Contain(topicName);
    }

    [Fact]
    public async Task ListTopics_ShouldReturnCreatedTopic()
    {
        // Arrange
        var topicName = $"test-topic-{Guid.NewGuid():N}";
        var createResponse = await _snsClient.CreateTopicAsync(new CreateTopicRequest
        {
            Name = topicName
        });

        // Act
        var listResponse = await _snsClient.ListTopicsAsync(new ListTopicsRequest());

        // Assert
        listResponse.Topics.Should().Contain(t => t.TopicArn == createResponse.TopicArn);
    }

    [Fact]
    public async Task PublishMessage_ShouldSucceed()
    {
        // Arrange
        var topicName = $"test-topic-{Guid.NewGuid():N}";
        var createResponse = await _snsClient.CreateTopicAsync(new CreateTopicRequest
        {
            Name = topicName
        });

        // Act
        var publishResponse = await _snsClient.PublishAsync(new PublishRequest
        {
            TopicArn = createResponse.TopicArn,
            Message = "Test message from integration test"
        });

        // Assert
        publishResponse.MessageId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task PublishMessage_WithSubject_ShouldSucceed()
    {
        // Arrange
        var topicName = $"test-topic-{Guid.NewGuid():N}";
        var createResponse = await _snsClient.CreateTopicAsync(new CreateTopicRequest
        {
            Name = topicName
        });

        // Act
        var publishResponse = await _snsClient.PublishAsync(new PublishRequest
        {
            TopicArn = createResponse.TopicArn,
            Subject = "Test Subject",
            Message = "Test message with subject"
        });

        // Assert
        publishResponse.MessageId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task PublishMessage_WithAttributes_ShouldSucceed()
    {
        // Arrange
        var topicName = $"test-topic-{Guid.NewGuid():N}";
        var createResponse = await _snsClient.CreateTopicAsync(new CreateTopicRequest
        {
            Name = topicName
        });

        var attributes = new Dictionary<string, MessageAttributeValue>
        {
            ["CustomAttribute"] = new MessageAttributeValue
            {
                DataType = "String",
                StringValue = "CustomValue"
            },
            ["Priority"] = new MessageAttributeValue
            {
                DataType = "Number",
                StringValue = "1"
            }
        };

        // Act
        var publishResponse = await _snsClient.PublishAsync(new PublishRequest
        {
            TopicArn = createResponse.TopicArn,
            Message = "Message with attributes",
            MessageAttributes = attributes
        });

        // Assert
        publishResponse.MessageId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task PublishBatch_ShouldSucceed()
    {
        // Arrange
        var topicName = $"test-topic-{Guid.NewGuid():N}";
        var createResponse = await _snsClient.CreateTopicAsync(new CreateTopicRequest
        {
            Name = topicName
        });

        var entries = Enumerable.Range(1, 5).Select(i => new PublishBatchRequestEntry
        {
            Id = i.ToString(),
            Message = $"Batch message {i}"
        }).ToList();

        // Act
        var batchResponse = await _snsClient.PublishBatchAsync(new PublishBatchRequest
        {
            TopicArn = createResponse.TopicArn,
            PublishBatchRequestEntries = entries
        });

        // Assert
        batchResponse.Successful.Should().HaveCount(5);
        batchResponse.Failed.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateFifoTopic_ShouldSucceed()
    {
        // Arrange
        var topicName = $"test-topic-{Guid.NewGuid():N}.fifo";

        // Act
        var response = await _snsClient.CreateTopicAsync(new CreateTopicRequest
        {
            Name = topicName,
            Attributes = new Dictionary<string, string>
            {
                ["FifoTopic"] = "true",
                ["ContentBasedDeduplication"] = "true"
            }
        });

        // Assert
        response.TopicArn.Should().NotBeNullOrEmpty();
        response.TopicArn.Should().EndWith(".fifo");
    }

    [Fact]
    public async Task GetTopicAttributes_ShouldReturnDetails()
    {
        // Arrange
        var topicName = $"test-topic-{Guid.NewGuid():N}";
        var createResponse = await _snsClient.CreateTopicAsync(new CreateTopicRequest
        {
            Name = topicName
        });

        // Act
        var attributesResponse = await _snsClient.GetTopicAttributesAsync(new GetTopicAttributesRequest
        {
            TopicArn = createResponse.TopicArn
        });

        // Assert
        attributesResponse.Attributes.Should().ContainKey("TopicArn");
        attributesResponse.Attributes["TopicArn"].Should().Be(createResponse.TopicArn);
    }

    [Fact]
    public async Task SetTopicAttributes_ShouldSucceed()
    {
        // Arrange
        var topicName = $"test-topic-{Guid.NewGuid():N}";
        var createResponse = await _snsClient.CreateTopicAsync(new CreateTopicRequest
        {
            Name = topicName
        });

        // Act
        await _snsClient.SetTopicAttributesAsync(new SetTopicAttributesRequest
        {
            TopicArn = createResponse.TopicArn,
            AttributeName = "DisplayName",
            AttributeValue = "Test Display Name"
        });

        var attributesResponse = await _snsClient.GetTopicAttributesAsync(new GetTopicAttributesRequest
        {
            TopicArn = createResponse.TopicArn
        });

        // Assert
        attributesResponse.Attributes["DisplayName"].Should().Be("Test Display Name");
    }

    [Fact]
    public async Task DeleteTopic_ShouldSucceed()
    {
        // Arrange
        var topicName = $"test-topic-{Guid.NewGuid():N}";
        var createResponse = await _snsClient.CreateTopicAsync(new CreateTopicRequest
        {
            Name = topicName
        });

        // Act
        await _snsClient.DeleteTopicAsync(new DeleteTopicRequest
        {
            TopicArn = createResponse.TopicArn
        });

        // Assert - Topic should no longer exist in list
        var listResponse = await _snsClient.ListTopicsAsync(new ListTopicsRequest());
        listResponse.Topics.Should().NotContain(t => t.TopicArn == createResponse.TopicArn);
    }

    [Fact]
    public async Task PublishJsonMessage_ShouldSucceed()
    {
        // Arrange
        var topicName = $"test-topic-{Guid.NewGuid():N}";
        var createResponse = await _snsClient.CreateTopicAsync(new CreateTopicRequest
        {
            Name = topicName
        });

        var jsonMessage = @"{
            ""default"": ""Default message"",
            ""email"": ""Email message"",
            ""sqs"": ""SQS message""
        }";

        // Act
        var publishResponse = await _snsClient.PublishAsync(new PublishRequest
        {
            TopicArn = createResponse.TopicArn,
            Message = jsonMessage,
            MessageStructure = "json"
        });

        // Assert
        publishResponse.MessageId.Should().NotBeNullOrEmpty();
    }
}
