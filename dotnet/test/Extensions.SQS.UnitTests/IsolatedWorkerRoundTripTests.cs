namespace Extensions.SQS.UnitTests;

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using Azure.Functions.Worker.Extensions.SQS;
using Azure.WebJobs.Extensions.SQS;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Core;
using Moq;
using Xunit;

/// <summary>
/// Round-trip tests that verify the host-side serialization (SqsQueueMessageValueProvider)
/// produces JSON that the worker-side converter (SqsMessageConverter) can deserialize back
/// into the original Message. This guards against schema drift between the two packages,
/// which was the root cause of the ParameterBindingData cast error that led to a broken
/// isolated-worker path.
/// </summary>
public class IsolatedWorkerRoundTripTests
{
    [Fact]
    public async Task IsolatedMode_ValueProviderReturnsParameterBindingData()
    {
        var message = CreateSampleMessage();
        var provider = new SqsQueueMessageValueProvider(message, isIsolatedWorker: true);

        provider.Type.Should().Be(typeof(Microsoft.Azure.WebJobs.ParameterBindingData));

        var value = await provider.GetValueAsync();
        value.Should().BeOfType<Microsoft.Azure.WebJobs.ParameterBindingData>();

        var pbd = (Microsoft.Azure.WebJobs.ParameterBindingData)value;
        pbd.Source.Should().Be(SqsQueueMessageValueProvider.BindingDataSource);
        pbd.ContentType.Should().Be(SqsQueueMessageValueProvider.JsonContentType);
        pbd.Version.Should().Be(SqsQueueMessageValueProvider.BindingDataVersion);
        pbd.Content.Should().NotBeNull();
    }

    [Fact]
    public async Task InProcessMode_ValueProviderReturnsRawMessage()
    {
        var message = CreateSampleMessage();
        var provider = new SqsQueueMessageValueProvider(message, isIsolatedWorker: false);

        provider.Type.Should().Be(typeof(Message));

        var value = await provider.GetValueAsync();
        value.Should().BeSameAs(message);
    }

    [Fact]
    public async Task RoundTrip_MessageSurvivesHostToWorkerSerialization()
    {
        // Arrange: create a message with a variety of fields populated
        var original = CreateSampleMessage();

        // Act: host-side serialization (what the value provider does in isolated mode)
        var provider = new SqsQueueMessageValueProvider(original, isIsolatedWorker: true);
        var hostValue = await provider.GetValueAsync();
        var hostPbd = (Microsoft.Azure.WebJobs.ParameterBindingData)hostValue;

        // Simulate: worker-side ModelBindingData with the same content (this is what the
        // Functions runtime does across the gRPC boundary between host and worker)
        var workerBindingData = CreateWorkerModelBindingData(
            content: hostPbd.Content,
            contentType: hostPbd.ContentType,
            source: hostPbd.Source,
            version: hostPbd.Version);

        // Act: worker-side conversion back to Message
        var converter = new SqsMessageConverter();
        var context = CreateConverterContext(workerBindingData, typeof(Message));
        var result = await converter.ConvertAsync(context);

        // Assert: conversion succeeded and the message matches
        result.Status.Should().Be(ConversionStatus.Succeeded);
        var roundTripped = result.Value.Should().BeOfType<Message>().Subject;
        roundTripped.MessageId.Should().Be(original.MessageId);
        roundTripped.ReceiptHandle.Should().Be(original.ReceiptHandle);
        roundTripped.Body.Should().Be(original.Body);
        roundTripped.MD5OfBody.Should().Be(original.MD5OfBody);
        roundTripped.MD5OfMessageAttributes.Should().Be(original.MD5OfMessageAttributes);
        roundTripped.Attributes.Should().Equal(original.Attributes);
        roundTripped.MessageAttributes.Should().HaveCount(original.MessageAttributes.Count);
        roundTripped.MessageAttributes["Priority"].StringValue.Should().Be("High");
        roundTripped.MessageAttributes["Priority"].DataType.Should().Be("String");
    }

    [Fact]
    public async Task RoundTrip_MessageWithBinaryAttributeSurvives()
    {
        var original = new Message
        {
            MessageId = "bin-1",
            Body = "hello",
            ReceiptHandle = "r1",
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                ["Payload"] = new MessageAttributeValue
                {
                    DataType = "Binary",
                    BinaryValue = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 })
                }
            }
        };

        var provider = new SqsQueueMessageValueProvider(original, isIsolatedWorker: true);
        var pbd = (Microsoft.Azure.WebJobs.ParameterBindingData)await provider.GetValueAsync();

        var workerBindingData = CreateWorkerModelBindingData(
            content: pbd.Content,
            contentType: pbd.ContentType,
            source: pbd.Source,
            version: pbd.Version);

        var converter = new SqsMessageConverter();
        var result = await converter.ConvertAsync(
            CreateConverterContext(workerBindingData, typeof(Message)));

        result.Status.Should().Be(ConversionStatus.Succeeded);
        var roundTripped = (Message)result.Value!;
        roundTripped.MessageAttributes["Payload"].DataType.Should().Be("Binary");
        var binaryBytes = new byte[5];
        var readCount = roundTripped.MessageAttributes["Payload"].BinaryValue.Read(binaryBytes, 0, 5);
        readCount.Should().Be(5);
        binaryBytes.Should().Equal(new byte[] { 1, 2, 3, 4, 5 });
    }

    [Fact]
    public async Task RoundTrip_MessageWithNoAttributesSurvives()
    {
        var original = new Message
        {
            MessageId = "empty-1",
            Body = "just a body",
            ReceiptHandle = "r2"
        };

        var provider = new SqsQueueMessageValueProvider(original, isIsolatedWorker: true);
        var pbd = (Microsoft.Azure.WebJobs.ParameterBindingData)await provider.GetValueAsync();

        var workerBindingData = CreateWorkerModelBindingData(pbd.Content, pbd.ContentType, pbd.Source, pbd.Version);
        var converter = new SqsMessageConverter();
        var result = await converter.ConvertAsync(CreateConverterContext(workerBindingData, typeof(Message)));

        result.Status.Should().Be(ConversionStatus.Succeeded);
        var roundTripped = (Message)result.Value!;
        roundTripped.MessageId.Should().Be("empty-1");
        roundTripped.Body.Should().Be("just a body");
        roundTripped.MessageAttributes.Should().BeEmpty();
    }

    [Fact]
    public async Task Converter_WithStringTargetType_ReturnsBody()
    {
        var original = CreateSampleMessage();
        var provider = new SqsQueueMessageValueProvider(original, isIsolatedWorker: true);
        var pbd = (Microsoft.Azure.WebJobs.ParameterBindingData)await provider.GetValueAsync();

        var workerBindingData = CreateWorkerModelBindingData(pbd.Content, pbd.ContentType, pbd.Source, pbd.Version);
        var converter = new SqsMessageConverter();
        var result = await converter.ConvertAsync(CreateConverterContext(workerBindingData, typeof(string)));

        result.Status.Should().Be(ConversionStatus.Succeeded);
        result.Value.Should().Be(original.Body);
    }

    [Fact]
    public async Task Converter_WithUnsupportedContentType_ReturnsUnhandled()
    {
        var workerBindingData = CreateWorkerModelBindingData(
            content: BinaryData.FromString("{}"),
            contentType: "text/plain",
            source: "AWSSQS",
            version: "1.0");

        var converter = new SqsMessageConverter();
        var result = await converter.ConvertAsync(CreateConverterContext(workerBindingData, typeof(Message)));

        result.Status.Should().Be(ConversionStatus.Unhandled);
    }

    private static Message CreateSampleMessage()
    {
        return new Message
        {
            MessageId = "msg-123",
            ReceiptHandle = "receipt-abc",
            Body = "Hello from SQS",
            MD5OfBody = "deadbeef",
            MD5OfMessageAttributes = "cafebabe",
            Attributes = new Dictionary<string, string>
            {
                ["SenderId"] = "sender-1",
                ["SentTimestamp"] = "1234567890"
            },
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                ["Priority"] = new MessageAttributeValue
                {
                    DataType = "String",
                    StringValue = "High"
                }
            }
        };
    }

    private static ModelBindingData CreateWorkerModelBindingData(
        BinaryData content, string contentType, string source, string version)
    {
        var mock = new Mock<ModelBindingData>();
        mock.SetupGet(m => m.Content).Returns(content);
        mock.SetupGet(m => m.ContentType).Returns(contentType);
        mock.SetupGet(m => m.Source).Returns(source);
        mock.SetupGet(m => m.Version).Returns(version);
        return mock.Object;
    }

    private static ConverterContext CreateConverterContext(object source, System.Type targetType)
    {
        var mock = new Mock<ConverterContext>();
        mock.SetupGet(c => c.Source).Returns(source);
        mock.SetupGet(c => c.TargetType).Returns(targetType);
        return mock.Object;
    }
}
