using Amazon.SQS;
using Amazon.SQS.Model;
using FluentAssertions;
using Xunit;

namespace Extensions.AWS.IntegrationTests;

[Collection("LocalStack")]
public class SqsIntegrationTests
{
    private readonly LocalStackFixture _fixture;
    private readonly AmazonSQSClient _sqsClient;

    public SqsIntegrationTests(LocalStackFixture fixture)
    {
        _fixture = fixture;
        _sqsClient = new AmazonSQSClient(
            _fixture.AccessKey,
            _fixture.SecretKey,
            new AmazonSQSConfig
            {
                ServiceURL = _fixture.Endpoint,
                AuthenticationRegion = _fixture.Region
            });
    }

    [Fact]
    public async Task CreateQueue_ShouldSucceed()
    {
        // Arrange
        var queueName = $"test-queue-{Guid.NewGuid():N}";

        // Act
        var response = await _sqsClient.CreateQueueAsync(new CreateQueueRequest
        {
            QueueName = queueName
        });

        // Assert
        response.QueueUrl.Should().NotBeNullOrEmpty();
        response.QueueUrl.Should().Contain(queueName);
    }

    [Fact]
    public async Task SendMessage_ShouldSucceed()
    {
        // Arrange
        var queueName = $"test-queue-{Guid.NewGuid():N}";
        var createResponse = await _sqsClient.CreateQueueAsync(new CreateQueueRequest
        {
            QueueName = queueName
        });
        var messageBody = "Test message content";

        // Act
        var sendResponse = await _sqsClient.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = createResponse.QueueUrl,
            MessageBody = messageBody
        });

        // Assert
        sendResponse.MessageId.Should().NotBeNullOrEmpty();
        sendResponse.MD5OfMessageBody.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SendAndReceiveMessage_ShouldReturnSameContent()
    {
        // Arrange
        var queueName = $"test-queue-{Guid.NewGuid():N}";
        var createResponse = await _sqsClient.CreateQueueAsync(new CreateQueueRequest
        {
            QueueName = queueName
        });
        var messageBody = "Hello from integration test!";

        // Act
        await _sqsClient.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = createResponse.QueueUrl,
            MessageBody = messageBody
        });

        var receiveResponse = await _sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl = createResponse.QueueUrl,
            MaxNumberOfMessages = 1,
            WaitTimeSeconds = 5
        });

        // Assert
        receiveResponse.Messages.Should().HaveCount(1);
        receiveResponse.Messages[0].Body.Should().Be(messageBody);
    }

    [Fact]
    public async Task SendMessageWithAttributes_ShouldPreserveAttributes()
    {
        // Arrange
        var queueName = $"test-queue-{Guid.NewGuid():N}";
        var createResponse = await _sqsClient.CreateQueueAsync(new CreateQueueRequest
        {
            QueueName = queueName
        });
        var messageBody = "Message with attributes";
        var attributes = new Dictionary<string, MessageAttributeValue>
        {
            ["CustomAttribute"] = new MessageAttributeValue
            {
                DataType = "String",
                StringValue = "CustomValue"
            },
            ["NumericAttribute"] = new MessageAttributeValue
            {
                DataType = "Number",
                StringValue = "42"
            }
        };

        // Act
        await _sqsClient.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = createResponse.QueueUrl,
            MessageBody = messageBody,
            MessageAttributes = attributes
        });

        var receiveResponse = await _sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl = createResponse.QueueUrl,
            MaxNumberOfMessages = 1,
            WaitTimeSeconds = 5,
            MessageAttributeNames = new List<string> { "All" }
        });

        // Assert
        receiveResponse.Messages.Should().HaveCount(1);
        var message = receiveResponse.Messages[0];
        message.MessageAttributes.Should().ContainKey("CustomAttribute");
        message.MessageAttributes["CustomAttribute"].StringValue.Should().Be("CustomValue");
        message.MessageAttributes.Should().ContainKey("NumericAttribute");
        message.MessageAttributes["NumericAttribute"].StringValue.Should().Be("42");
    }

    [Fact]
    public async Task SendBatchMessages_ShouldSucceed()
    {
        // Arrange
        var queueName = $"test-queue-{Guid.NewGuid():N}";
        var createResponse = await _sqsClient.CreateQueueAsync(new CreateQueueRequest
        {
            QueueName = queueName
        });

        var entries = Enumerable.Range(1, 5).Select(i => new SendMessageBatchRequestEntry
        {
            Id = i.ToString(),
            MessageBody = $"Batch message {i}"
        }).ToList();

        // Act
        var batchResponse = await _sqsClient.SendMessageBatchAsync(new SendMessageBatchRequest
        {
            QueueUrl = createResponse.QueueUrl,
            Entries = entries
        });

        // Assert
        batchResponse.Successful.Should().HaveCount(5);
        batchResponse.Failed.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteMessage_ShouldRemoveFromQueue()
    {
        // Arrange
        var queueName = $"test-queue-{Guid.NewGuid():N}";
        var createResponse = await _sqsClient.CreateQueueAsync(new CreateQueueRequest
        {
            QueueName = queueName
        });

        await _sqsClient.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = createResponse.QueueUrl,
            MessageBody = "Message to delete"
        });

        var receiveResponse = await _sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl = createResponse.QueueUrl,
            MaxNumberOfMessages = 1,
            WaitTimeSeconds = 5
        });

        // Act
        await _sqsClient.DeleteMessageAsync(new DeleteMessageRequest
        {
            QueueUrl = createResponse.QueueUrl,
            ReceiptHandle = receiveResponse.Messages[0].ReceiptHandle
        });

        // Assert - Queue should be empty now
        var secondReceive = await _sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl = createResponse.QueueUrl,
            MaxNumberOfMessages = 1,
            WaitTimeSeconds = 1
        });
        secondReceive.Messages.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateFifoQueue_ShouldSucceed()
    {
        // Arrange
        var queueName = $"test-queue-{Guid.NewGuid():N}.fifo";

        // Act
        var response = await _sqsClient.CreateQueueAsync(new CreateQueueRequest
        {
            QueueName = queueName,
            Attributes = new Dictionary<string, string>
            {
                ["FifoQueue"] = "true",
                ["ContentBasedDeduplication"] = "true"
            }
        });

        // Assert
        response.QueueUrl.Should().NotBeNullOrEmpty();
        response.QueueUrl.Should().EndWith(".fifo");
    }

    [Fact]
    public async Task SendFifoMessage_ShouldRespectOrdering()
    {
        // Arrange
        var queueName = $"test-queue-{Guid.NewGuid():N}.fifo";
        var createResponse = await _sqsClient.CreateQueueAsync(new CreateQueueRequest
        {
            QueueName = queueName,
            Attributes = new Dictionary<string, string>
            {
                ["FifoQueue"] = "true",
                ["ContentBasedDeduplication"] = "true"
            }
        });

        // Act - Send messages with message group
        for (int i = 1; i <= 3; i++)
        {
            await _sqsClient.SendMessageAsync(new SendMessageRequest
            {
                QueueUrl = createResponse.QueueUrl,
                MessageBody = $"Message {i}",
                MessageGroupId = "test-group"
            });
        }

        // Receive messages
        var messages = new List<Message>();
        for (int i = 0; i < 3; i++)
        {
            var response = await _sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
            {
                QueueUrl = createResponse.QueueUrl,
                MaxNumberOfMessages = 1,
                WaitTimeSeconds = 5
            });
            if (response.Messages.Any())
            {
                messages.Add(response.Messages[0]);
                await _sqsClient.DeleteMessageAsync(createResponse.QueueUrl, response.Messages[0].ReceiptHandle);
            }
        }

        // Assert - Messages should be in order
        messages.Should().HaveCount(3);
        messages[0].Body.Should().Be("Message 1");
        messages[1].Body.Should().Be("Message 2");
        messages[2].Body.Should().Be("Message 3");
    }

    [Fact]
    public async Task GetQueueAttributes_ShouldReturnDetails()
    {
        // Arrange
        var queueName = $"test-queue-{Guid.NewGuid():N}";
        var createResponse = await _sqsClient.CreateQueueAsync(new CreateQueueRequest
        {
            QueueName = queueName
        });

        // Act
        var attributesResponse = await _sqsClient.GetQueueAttributesAsync(new GetQueueAttributesRequest
        {
            QueueUrl = createResponse.QueueUrl,
            AttributeNames = new List<string> { "All" }
        });

        // Assert
        attributesResponse.Attributes.Should().ContainKey("QueueArn");
        attributesResponse.Attributes["QueueArn"].Should().Contain(queueName);
    }
}
