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

    #endregion

    #region StopAsync Tests

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
    [Fact]
    public async Task Dispose_WhenCalledConcurrently_ShouldBeThreadSafe()
    {
        // Arrange
        var attribute = CreateValidAttribute();
        var options = CreateDefaultOptions();
        var executor = new Mock<ITriggeredFunctionExecutor>();
        var listener = new SqsQueueTriggerListener(attribute, options, executor.Object);

        // Act - Call Dispose from multiple threads simultaneously
        var tasks = Enumerable.Range(0, 10).Select(_ => Task.Run(() => listener.Dispose()));

        // Assert - Should not throw any exceptions due to race conditions
        var action = async () => await Task.WhenAll(tasks);
        await action.Should().NotThrowAsync("Dispose should be thread-safe");
    }

    /// <summary>
    /// BUG: Dispose during active message processing may lose messages.
    /// When StopAsync is called during message processing, pending messages
    /// may not be returned to the queue if Dispose is called immediately after.
    /// 
    /// GitHub Issue: https://github.com/laveeshb/azure-functions-sqs-extension/issues/40
    /// Fix: Implement graceful shutdown that waits for in-flight messages to complete.
    /// 
    /// NOTE: This test is skipped because it requires an integration test with a real
    /// SQS connection to properly simulate in-flight message processing.
    /// </summary>
    [Fact(Skip = "Requires integration test - cannot simulate in-flight messages with mocks")]
    public async Task Dispose_DuringMessageProcessing_ShouldWaitForCompletion()
    {
        // This scenario cannot be properly tested as a unit test.
        // The listener needs to actually connect to SQS and receive messages
        // to have "in-flight" messages during dispose.
        // 
        // See: dotnet/test/Extensions.SQS.Test.InProcess for integration tests
        await Task.CompletedTask;
    }

    #endregion
}
