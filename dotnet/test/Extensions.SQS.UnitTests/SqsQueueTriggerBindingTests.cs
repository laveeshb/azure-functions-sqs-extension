namespace Extensions.SQS.UnitTests;

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using Azure.WebJobs.Extensions.SQS;
using FluentAssertions;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

public class SqsQueueTriggerBindingTests
{
    private const string ValidQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/my-queue";

    private static ParameterInfo GetSampleParameterInfo()
    {
        // Get a sample ParameterInfo from a dummy method
        return typeof(SqsQueueTriggerBindingTests)
            .GetMethod(nameof(SampleMethod), BindingFlags.NonPublic | BindingFlags.Static)!
            .GetParameters()[0];
    }

    private static void SampleMethod(Message message) { }

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
    public void Constructor_WithValidParameters_CreatesBinding()
    {
        // Arrange
        var parameterInfo = GetSampleParameterInfo();
        var attribute = CreateValidAttribute();
        var options = CreateDefaultOptions();

        // Act
        var binding = new SqsQueueTriggerBinding(parameterInfo, attribute, options);

        // Assert
        binding.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullParameterInfo_ThrowsArgumentNullException()
    {
        // Arrange
        var attribute = CreateValidAttribute();
        var options = CreateDefaultOptions();

        // Act & Assert
        var action = () => new SqsQueueTriggerBinding(null!, attribute, options);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("parameterInfo");
    }

    [Fact]
    public void Constructor_WithNullAttribute_ThrowsArgumentNullException()
    {
        // Arrange
        var parameterInfo = GetSampleParameterInfo();
        var options = CreateDefaultOptions();

        // Act & Assert
        var action = () => new SqsQueueTriggerBinding(parameterInfo, null!, options);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("triggerParameters");
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var parameterInfo = GetSampleParameterInfo();
        var attribute = CreateValidAttribute();

        // Act & Assert
        var action = () => new SqsQueueTriggerBinding(parameterInfo, attribute, null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("sqsQueueOptions");
    }

    #endregion

    #region TriggerValueType Tests

    [Fact]
    public void TriggerValueType_ReturnsMessageType()
    {
        // Arrange
        var parameterInfo = GetSampleParameterInfo();
        var attribute = CreateValidAttribute();
        var options = CreateDefaultOptions();
        var binding = new SqsQueueTriggerBinding(parameterInfo, attribute, options);

        // Act
        var type = binding.TriggerValueType;

        // Assert
        type.Should().Be(typeof(Message));
    }

    #endregion

    #region BindingDataContract Tests

    [Fact]
    public void BindingDataContract_ReturnsEmptyDictionary()
    {
        // Arrange
        var parameterInfo = GetSampleParameterInfo();
        var attribute = CreateValidAttribute();
        var options = CreateDefaultOptions();
        var binding = new SqsQueueTriggerBinding(parameterInfo, attribute, options);

        // Act
        var contract = binding.BindingDataContract;

        // Assert
        contract.Should().NotBeNull();
        contract.Should().BeEmpty();
    }

    #endregion

    #region BindAsync Tests

    [Fact]
    public async Task BindAsync_WithMessage_ReturnsTriggerData()
    {
        // Arrange
        var parameterInfo = GetSampleParameterInfo();
        var attribute = CreateValidAttribute();
        var options = CreateDefaultOptions();
        var binding = new SqsQueueTriggerBinding(parameterInfo, attribute, options);
        
        var message = new Message
        {
            MessageId = "test-id",
            Body = "test body"
        };

        // Act - BindAsync doesn't actually use the context for our implementation
        // so we can pass null (cast to satisfy compiler)
        var result = await binding.BindAsync(message, null!);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<ITriggerData>();
    }

    [Fact]
    public async Task BindAsync_ReturnsTriggerDataWithValueProvider()
    {
        // Arrange
        var parameterInfo = GetSampleParameterInfo();
        var attribute = CreateValidAttribute();
        var options = CreateDefaultOptions();
        var binding = new SqsQueueTriggerBinding(parameterInfo, attribute, options);
        
        var message = new Message { Body = "test" };

        // Act
        var result = await binding.BindAsync(message, null!);

        // Assert
        result.ValueProvider.Should().NotBeNull();
        var value = await result.ValueProvider.GetValueAsync();
        value.Should().BeSameAs(message);
    }

    [Fact]
    public async Task BindAsync_ReturnsTriggerDataWithEmptyBindingData()
    {
        // Arrange
        var parameterInfo = GetSampleParameterInfo();
        var attribute = CreateValidAttribute();
        var options = CreateDefaultOptions();
        var binding = new SqsQueueTriggerBinding(parameterInfo, attribute, options);
        
        var message = new Message { Body = "test" };

        // Act
        var result = await binding.BindAsync(message, null!);

        // Assert
        result.BindingData.Should().NotBeNull();
        result.BindingData.Should().BeEmpty();
    }

    #endregion

    #region CreateListenerAsync Tests

    [Fact]
    public async Task CreateListenerAsync_ReturnsListener()
    {
        // Arrange
        var parameterInfo = GetSampleParameterInfo();
        var attribute = CreateValidAttribute();
        var options = CreateDefaultOptions();
        var binding = new SqsQueueTriggerBinding(parameterInfo, attribute, options);

        var executorMock = new Mock<ITriggeredFunctionExecutor>();
        var listenerContext = new ListenerFactoryContext(
            new FunctionDescriptor { Id = "test" },
            executorMock.Object,
            CancellationToken.None);

        // Act
        var listener = await binding.CreateListenerAsync(listenerContext);

        // Assert
        listener.Should().NotBeNull();
        listener.Should().BeOfType<SqsQueueTriggerListener>();
        
        // Cleanup
        listener.Dispose();
    }

    #endregion

    #region ToParameterDescriptor Tests

    [Fact]
    public void ToParameterDescriptor_ReturnsDescriptorWithParameterName()
    {
        // Arrange
        var parameterInfo = GetSampleParameterInfo();
        var attribute = CreateValidAttribute();
        var options = CreateDefaultOptions();
        var binding = new SqsQueueTriggerBinding(parameterInfo, attribute, options);

        // Act
        var descriptor = binding.ToParameterDescriptor();

        // Assert
        descriptor.Should().NotBeNull();
        descriptor.Name.Should().Be("message"); // Name from SampleMethod parameter
    }

    #endregion
}

// Required mock classes for testing
public class FunctionDescriptor : Microsoft.Azure.WebJobs.Host.Protocols.FunctionDescriptor
{
}
