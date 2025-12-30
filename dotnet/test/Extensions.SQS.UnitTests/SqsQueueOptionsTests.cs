namespace Extensions.SQS.UnitTests;

using System;
using Azure.WebJobs.Extensions.SQS;
using FluentAssertions;
using Xunit;

public class SqsQueueOptionsTests
{
    #region Default Values Tests

    [Fact]
    public void MaxNumberOfMessages_DefaultValue_IsNull()
    {
        // Arrange & Act
        var options = new SqsQueueOptions();

        // Assert
        options.MaxNumberOfMessages.Should().BeNull();
    }

    [Fact]
    public void PollingInterval_DefaultValue_IsNull()
    {
        // Arrange & Act
        var options = new SqsQueueOptions();

        // Assert
        options.PollingInterval.Should().BeNull();
    }

    [Fact]
    public void VisibilityTimeout_DefaultValue_IsNull()
    {
        // Arrange & Act
        var options = new SqsQueueOptions();

        // Assert
        options.VisibilityTimeout.Should().BeNull();
    }

    #endregion

    #region Property Assignment Tests

    [Fact]
    public void MaxNumberOfMessages_CanBeSet()
    {
        // Arrange
        var options = new SqsQueueOptions();

        // Act
        options.MaxNumberOfMessages = 10;

        // Assert
        options.MaxNumberOfMessages.Should().Be(10);
    }

    [Fact]
    public void PollingInterval_CanBeSet()
    {
        // Arrange
        var options = new SqsQueueOptions();
        var interval = TimeSpan.FromSeconds(15);

        // Act
        options.PollingInterval = interval;

        // Assert
        options.PollingInterval.Should().Be(interval);
    }

    [Fact]
    public void VisibilityTimeout_CanBeSet()
    {
        // Arrange
        var options = new SqsQueueOptions();
        var timeout = TimeSpan.FromMinutes(5);

        // Act
        options.VisibilityTimeout = timeout;

        // Assert
        options.VisibilityTimeout.Should().Be(timeout);
    }

    #endregion

    #region Valid Values Tests

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void MaxNumberOfMessages_AcceptsValidValues(int value)
    {
        // Arrange
        var options = new SqsQueueOptions();

        // Act
        options.MaxNumberOfMessages = value;

        // Assert
        options.MaxNumberOfMessages.Should().Be(value);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(30)]
    [InlineData(60)]
    public void PollingInterval_AcceptsVariousSeconds(int seconds)
    {
        // Arrange
        var options = new SqsQueueOptions();

        // Act
        options.PollingInterval = TimeSpan.FromSeconds(seconds);

        // Assert
        options.PollingInterval.Should().Be(TimeSpan.FromSeconds(seconds));
    }

    [Theory]
    [InlineData(30)]
    [InlineData(60)]
    [InlineData(300)]
    [InlineData(43200)] // 12 hours - max SQS allows
    public void VisibilityTimeout_AcceptsVariousSeconds(int seconds)
    {
        // Arrange
        var options = new SqsQueueOptions();

        // Act
        options.VisibilityTimeout = TimeSpan.FromSeconds(seconds);

        // Assert
        options.VisibilityTimeout.Should().Be(TimeSpan.FromSeconds(seconds));
    }

    #endregion
}
