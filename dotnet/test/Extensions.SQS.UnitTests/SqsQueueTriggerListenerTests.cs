namespace Extensions.SQS.UnitTests;

using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.WebJobs.Extensions.SQS;
using FluentAssertions;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

public class SqsQueueTriggerListenerTests
{
    private const string ValidQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/my-queue";

    private static SqsQueueTriggerAttribute CreateValidAttribute() => new()
    {
        QueueUrl = ValidQueueUrl
    };

    private static IOptions<SqsQueueOptions> CreateDefaultOptions() =>
        Options.Create(new SqsQueueOptions
        {
            MaxNumberOfMessages = 5,
            PollingInterval = TimeSpan.FromSeconds(5),
            VisibilityTimeout = TimeSpan.FromSeconds(30)
        });

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_CreatesListener()
    {
        // Arrange
        var attribute = CreateValidAttribute();
        var options = CreateDefaultOptions();
        var executor = new Mock<ITriggeredFunctionExecutor>();

        // Act
        var listener = new SqsQueueTriggerListener(
            attribute,
            options,
            executor.Object);

        // Assert
        listener.Should().NotBeNull();
        
        // Cleanup
        listener.Dispose();
    }

    [Fact]
    public void Constructor_WithNullAttribute_ThrowsArgumentNullException()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var executor = new Mock<ITriggeredFunctionExecutor>();

        // Act & Assert
        var action = () => new SqsQueueTriggerListener(
            null!,
            options,
            executor.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("triggerParameters");
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var attribute = CreateValidAttribute();
        var executor = new Mock<ITriggeredFunctionExecutor>();

        // Act & Assert
        var action = () => new SqsQueueTriggerListener(
            attribute,
            null!,
            executor.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("sqsQueueOptions");
    }

    [Fact]
    public void Constructor_WithNullExecutor_ThrowsArgumentNullException()
    {
        // Arrange
        var attribute = CreateValidAttribute();
        var options = CreateDefaultOptions();

        // Act & Assert
        var action = () => new SqsQueueTriggerListener(
            attribute,
            options,
            null!);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("executor");
    }

    [Fact]
    public void Constructor_WithNullLogger_DoesNotThrow()
    {
        // Arrange
        var attribute = CreateValidAttribute();
        var options = CreateDefaultOptions();
        var executor = new Mock<ITriggeredFunctionExecutor>();

        // Act & Assert - Logger is optional
        var action = () => new SqsQueueTriggerListener(
            attribute,
            options,
            executor.Object,
            logger: null);

        action.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithLogger_AcceptsLogger()
    {
        // Arrange
        var attribute = CreateValidAttribute();
        var options = CreateDefaultOptions();
        var executor = new Mock<ITriggeredFunctionExecutor>();
        var logger = new Mock<ILogger>();

        // Act
        var listener = new SqsQueueTriggerListener(
            attribute,
            options,
            executor.Object,
            logger.Object);

        // Assert
        listener.Should().NotBeNull();
        
        // Cleanup
        listener.Dispose();
    }

    #endregion

    #region Default Options Tests

    [Fact]
    public void Constructor_WithNullMaxNumberOfMessages_SetsDefaultValue()
    {
        // Arrange
        var attribute = CreateValidAttribute();
        var options = Options.Create(new SqsQueueOptions
        {
            MaxNumberOfMessages = null, // Should default to 5
            PollingInterval = TimeSpan.FromSeconds(5),
            VisibilityTimeout = TimeSpan.FromSeconds(30)
        });
        var executor = new Mock<ITriggeredFunctionExecutor>();

        // Act
        var listener = new SqsQueueTriggerListener(
            attribute,
            options,
            executor.Object);

        // Assert
        options.Value.MaxNumberOfMessages.Should().Be(5);
        
        // Cleanup
        listener.Dispose();
    }

    [Fact]
    public void Constructor_WithNullPollingInterval_SetsDefaultValue()
    {
        // Arrange
        var attribute = CreateValidAttribute();
        var options = Options.Create(new SqsQueueOptions
        {
            MaxNumberOfMessages = 5,
            PollingInterval = null, // Should default to 5 seconds
            VisibilityTimeout = TimeSpan.FromSeconds(30)
        });
        var executor = new Mock<ITriggeredFunctionExecutor>();

        // Act
        var listener = new SqsQueueTriggerListener(
            attribute,
            options,
            executor.Object);

        // Assert
        options.Value.PollingInterval.Should().Be(TimeSpan.FromSeconds(5));
        
        // Cleanup
        listener.Dispose();
    }

    [Fact]
    public void Constructor_WithNullVisibilityTimeout_SetsDefaultValue()
    {
        // Arrange
        var attribute = CreateValidAttribute();
        var options = Options.Create(new SqsQueueOptions
        {
            MaxNumberOfMessages = 5,
            PollingInterval = TimeSpan.FromSeconds(5),
            VisibilityTimeout = null // Should default to 30 seconds
        });
        var executor = new Mock<ITriggeredFunctionExecutor>();

        // Act
        var listener = new SqsQueueTriggerListener(
            attribute,
            options,
            executor.Object);

        // Assert
        options.Value.VisibilityTimeout.Should().Be(TimeSpan.FromSeconds(30));
        
        // Cleanup
        listener.Dispose();
    }

    #endregion

    #region StartAsync Tests

    [Fact]
    public async Task StartAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var attribute = CreateValidAttribute();
        var options = CreateDefaultOptions();
        var executor = new Mock<ITriggeredFunctionExecutor>();
        var listener = new SqsQueueTriggerListener(attribute, options, executor.Object);
        listener.Dispose();

        // Act & Assert
        var action = async () => await listener.StartAsync(CancellationToken.None);
        await action.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task StartAsync_ReturnsCompletedTask()
    {
        // Arrange
        var attribute = CreateValidAttribute();
        var options = CreateDefaultOptions();
        var executor = new Mock<ITriggeredFunctionExecutor>();
        using var listener = new SqsQueueTriggerListener(attribute, options, executor.Object);

        // Act
        var startTask = listener.StartAsync(CancellationToken.None);

        // Assert
        await startTask; // Should complete without error (starts background polling)
        
        // Cleanup - stop immediately
        await listener.StopAsync(CancellationToken.None);
    }

    #endregion

    #region StopAsync Tests

    [Fact]
    public async Task StopAsync_AfterStart_StopsGracefully()
    {
        // Arrange
        var attribute = CreateValidAttribute();
        var options = CreateDefaultOptions();
        var executor = new Mock<ITriggeredFunctionExecutor>();
        using var listener = new SqsQueueTriggerListener(attribute, options, executor.Object);

        // Act
        await listener.StartAsync(CancellationToken.None);
        
        // Wait a small amount to let polling start
        await Task.Delay(100);
        
        // Should stop gracefully
        var stopAction = async () => await listener.StopAsync(CancellationToken.None);

        // Assert
        await stopAction.Should().CompleteWithinAsync(TimeSpan.FromSeconds(35)); // 30s graceful + 5s buffer
    }

    [Fact]
    public async Task StopAsync_WithoutStart_DoesNotThrow()
    {
        // Arrange
        var attribute = CreateValidAttribute();
        var options = CreateDefaultOptions();
        var executor = new Mock<ITriggeredFunctionExecutor>();
        using var listener = new SqsQueueTriggerListener(attribute, options, executor.Object);

        // Act & Assert - Should not throw even if never started
        var action = async () => await listener.StopAsync(CancellationToken.None);
        await action.Should().NotThrowAsync();
    }

    #endregion

    #region Cancel Tests

    [Fact]
    public void Cancel_BeforeStart_DoesNotThrow()
    {
        // Arrange
        var attribute = CreateValidAttribute();
        var options = CreateDefaultOptions();
        var executor = new Mock<ITriggeredFunctionExecutor>();
        using var listener = new SqsQueueTriggerListener(attribute, options, executor.Object);

        // Act & Assert - Should not throw
        var action = () => listener.Cancel();
        action.Should().NotThrow();
    }

    [Fact]
    public async Task Cancel_AfterStart_CancelsPolling()
    {
        // Arrange
        var attribute = CreateValidAttribute();
        var options = CreateDefaultOptions();
        var executor = new Mock<ITriggeredFunctionExecutor>();
        using var listener = new SqsQueueTriggerListener(attribute, options, executor.Object);

        await listener.StartAsync(CancellationToken.None);

        // Act & Assert - Should not throw
        var action = () => listener.Cancel();
        action.Should().NotThrow();
        
        // Cleanup
        await listener.StopAsync(CancellationToken.None);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var attribute = CreateValidAttribute();
        var options = CreateDefaultOptions();
        var executor = new Mock<ITriggeredFunctionExecutor>();
        var listener = new SqsQueueTriggerListener(attribute, options, executor.Object);

        // Act & Assert - Should not throw
        var action = () =>
        {
            listener.Dispose();
            listener.Dispose();
            listener.Dispose();
        };

        action.Should().NotThrow();
    }

    [Fact]
    public async Task Dispose_AfterStart_CleansUpResources()
    {
        // Arrange
        var attribute = CreateValidAttribute();
        var options = CreateDefaultOptions();
        var executor = new Mock<ITriggeredFunctionExecutor>();
        var listener = new SqsQueueTriggerListener(attribute, options, executor.Object);

        await listener.StartAsync(CancellationToken.None);

        // Act
        listener.Dispose();

        // Assert - StartAsync should now throw ObjectDisposedException
        var action = async () => await listener.StartAsync(CancellationToken.None);
        await action.Should().ThrowAsync<ObjectDisposedException>();
    }

    #endregion

    #region Known Bugs - Expected Failures

    /// <summary>
    /// BUG: Dispose is not thread-safe - concurrent calls may cause race condition.
    /// The _disposed flag check and set are not atomic, leading to potential double-dispose
    /// of internal resources like _cancellationTokenSource and _sqsClient.
    /// 
    /// GitHub Issue: https://github.com/laveeshb/azure-functions-sqs-extension/issues/40
    /// Fix: Use Interlocked.Exchange for thread-safe dispose pattern.
    /// </summary>
    [Fact(Skip = "Known bug #40: Dispose race condition - not thread-safe")]
    public void Dispose_WhenCalledConcurrently_ShouldBeThreadSafe()
    {
        // Arrange
        var attribute = CreateValidAttribute();
        var options = CreateDefaultOptions();
        var executor = new Mock<ITriggeredFunctionExecutor>();
        var listener = new SqsQueueTriggerListener(attribute, options, executor.Object);

        // Act - Call Dispose from multiple threads simultaneously
        var tasks = new Task[100];
        var barrier = new Barrier(100);

        for (int i = 0; i < 100; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                barrier.SignalAndWait(); // Ensure all threads start together
                listener.Dispose();
            });
        }

        // Assert - Should not throw any exceptions due to race conditions
        // Currently this may throw ObjectDisposedException or NullReferenceException
        // when _cancellationTokenSource is disposed twice or accessed after null
        var action = () => Task.WaitAll(tasks);
        action.Should().NotThrow("Dispose should be thread-safe");
    }

    /// <summary>
    /// BUG: Dispose during active message processing may lose messages.
    /// When StopAsync is called during message processing, pending messages
    /// may not be returned to the queue if Dispose is called immediately after.
    /// 
    /// GitHub Issue: https://github.com/laveeshb/azure-functions-sqs-extension/issues/40
    /// Fix: Implement graceful shutdown that waits for in-flight messages to complete.
    /// </summary>
    [Fact(Skip = "Known bug #40: Dispose may lose in-flight messages")]
    public async Task Dispose_DuringMessageProcessing_ShouldWaitForCompletion()
    {
        // Arrange
        var attribute = CreateValidAttribute();
        var options = CreateDefaultOptions();
        var messageProcessingComplete = new TaskCompletionSource<bool>();
        var messageProcessingStarted = new TaskCompletionSource<bool>();

        var executor = new Mock<ITriggeredFunctionExecutor>();
        executor.Setup(x => x.TryExecuteAsync(
                It.IsAny<TriggeredFunctionData>(),
                It.IsAny<CancellationToken>()))
            .Returns(async (TriggeredFunctionData data, CancellationToken ct) =>
            {
                messageProcessingStarted.SetResult(true);
                await messageProcessingComplete.Task; // Wait until we're told to complete
                return new FunctionResult(true);
            });

        var listener = new SqsQueueTriggerListener(attribute, options, executor.Object);
        await listener.StartAsync(CancellationToken.None);

        // Simulate that message processing has started
        await messageProcessingStarted.Task;

        // Act - Dispose while message is being processed
        var disposeTask = Task.Run(() => listener.Dispose());

        // Allow message to complete
        messageProcessingComplete.SetResult(true);

        // Assert - Dispose should wait for message processing to complete
        // Currently Dispose may return immediately, leaving message unacknowledged
        await disposeTask;
        
        // Verify message was fully processed
        executor.Verify(x => x.TryExecuteAsync(
            It.IsAny<TriggeredFunctionData>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
