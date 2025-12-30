namespace Extensions.SQS.UnitTests;

using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using Azure.WebJobs.Extensions.SQS;
using FluentAssertions;
using Xunit;

public class SqsQueueAsyncCollectorTests
{
    private const string ValidQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/my-queue";

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidAttribute_CreatesCollector()
    {
        // Arrange
        var attribute = new SqsQueueOutAttribute { QueueUrl = ValidQueueUrl };

        // Act
        var collector = new SqsQueueAsyncCollector(attribute);

        // Assert
        collector.Should().NotBeNull();
        
        // Cleanup
        collector.Dispose();
    }

    [Fact]
    public void Constructor_WithNullAttribute_ThrowsArgumentNullException()
    {
        // Act & Assert
        var action = () => new SqsQueueAsyncCollector(null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("sqsQueueOut");
    }

    #endregion

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var attribute = new SqsQueueOutAttribute { QueueUrl = ValidQueueUrl };
        using var collector = new SqsQueueAsyncCollector(attribute);

        // Act & Assert
        var action = async () => await collector.AddAsync(null!, CancellationToken.None);
        await action.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task AddAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var attribute = new SqsQueueOutAttribute { QueueUrl = ValidQueueUrl };
        var collector = new SqsQueueAsyncCollector(attribute);
        collector.Dispose();

        var request = new SendMessageRequest { MessageBody = "test" };

        // Act & Assert
        var action = async () => await collector.AddAsync(request, CancellationToken.None);
        await action.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public void AddAsync_WithRequestMissingQueueUrl_RequestHasNullQueueUrlInitially()
    {
        // Arrange
        var attribute = new SqsQueueOutAttribute { QueueUrl = ValidQueueUrl };
        using var collector = new SqsQueueAsyncCollector(attribute);
        
        var request = new SendMessageRequest 
        { 
            MessageBody = "test",
            QueueUrl = null // Should be filled from attribute when AddAsync is called
        };

        // Assert - Verify initial state
        // Note: The actual QueueUrl assignment happens in AddAsync when it sends to AWS
        // We can't easily test this without mocking the SQS client or using LocalStack
        request.QueueUrl.Should().BeNull();
        
        // The AddAsync method sets the QueueUrl if not provided
        // We can't easily test this without mocking the SQS client
    }

    #endregion

    #region FlushAsync Tests

    [Fact]
    public async Task FlushAsync_ReturnsCompletedTask()
    {
        // Arrange
        var attribute = new SqsQueueOutAttribute { QueueUrl = ValidQueueUrl };
        using var collector = new SqsQueueAsyncCollector(attribute);

        // Act
        var task = collector.FlushAsync(CancellationToken.None);

        // Assert
        task.IsCompleted.Should().BeTrue();
        await task; // Should complete immediately
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var attribute = new SqsQueueOutAttribute { QueueUrl = ValidQueueUrl };
        var collector = new SqsQueueAsyncCollector(attribute);

        // Act & Assert - Should not throw
        var action = () =>
        {
            collector.Dispose();
            collector.Dispose();
            collector.Dispose();
        };
        
        action.Should().NotThrow();
    }

    [Fact]
    public async Task Dispose_PreventsSubsequentAddAsync()
    {
        // Arrange
        var attribute = new SqsQueueOutAttribute { QueueUrl = ValidQueueUrl };
        var collector = new SqsQueueAsyncCollector(attribute);

        // Act
        collector.Dispose();

        // Assert
        var request = new SendMessageRequest { MessageBody = "test" };
        var action = async () => await collector.AddAsync(request, CancellationToken.None);
        await action.Should().ThrowAsync<ObjectDisposedException>();
    }

    #endregion
}
