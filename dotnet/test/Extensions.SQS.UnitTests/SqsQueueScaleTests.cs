namespace Extensions.SQS.UnitTests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Azure.WebJobs.Extensions.SQS;
using FluentAssertions;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

public class SqsQueueScaleTests
{
    private const string QueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/my-queue";
    private const string FunctionId = "MyFunction";

    private static Mock<IAmazonSQS> ClientReturning(int visible, int inFlight)
    {
        var client = new Mock<IAmazonSQS>();
        client.Setup(c => c.GetQueueAttributesAsync(It.IsAny<GetQueueAttributesRequest>(), default))
            .ReturnsAsync(new GetQueueAttributesResponse
            {
                Attributes = new Dictionary<string, string>
                {
                    ["ApproximateNumberOfMessages"] = visible.ToString(),
                    ["ApproximateNumberOfMessagesNotVisible"] = inFlight.ToString()
                }
            });
        return client;
    }

    private static ScaleStatusContext<SqsQueueScaleMetrics> Context(int workers, params long[] lengths) => new()
    {
        WorkerCount = workers,
        Metrics = lengths.Select(l => new SqsQueueScaleMetrics { QueueLength = l }).ToArray()
    };

    private static SqsQueueScaleMonitor Monitor(IAmazonSQS client, int target = 10) =>
        new(FunctionId, client, QueueUrl, target);

    #region Monitor

    [Fact]
    public async Task GetMetricsAsync_SumsVisibleAndInFlight()
    {
        var monitor = Monitor(ClientReturning(7, 3).Object);

        var metrics = await monitor.GetMetricsAsync();

        metrics.QueueLength.Should().Be(10);
    }

    [Fact]
    public async Task GetMetricsAsync_RequestsBothCounters()
    {
        var client = ClientReturning(1, 1);
        var monitor = Monitor(client.Object);

        await monitor.GetMetricsAsync();

        client.Verify(c => c.GetQueueAttributesAsync(
            It.Is<GetQueueAttributesRequest>(r =>
                r.QueueUrl == QueueUrl
                && r.AttributeNames.Contains("ApproximateNumberOfMessages")
                && r.AttributeNames.Contains("ApproximateNumberOfMessagesNotVisible")),
            default), Times.Once);
    }

    [Fact]
    public void Descriptor_UsesFunctionId()
    {
        var monitor = Monitor(ClientReturning(0, 0).Object);

        monitor.Descriptor.FunctionId.Should().Be(FunctionId);
        monitor.Descriptor.Id.Should().Contain(FunctionId);
    }

    [Fact]
    public void GetScaleStatus_NoMetrics_VotesNone()
    {
        var status = Monitor(ClientReturning(0, 0).Object).GetScaleStatus(Context(1));

        status.Vote.Should().Be(ScaleVote.None);
    }

    [Fact]
    public void GetScaleStatus_MessagesAndNoWorkers_VotesScaleOut()
    {
        var status = Monitor(ClientReturning(0, 0).Object).GetScaleStatus(Context(0, 1));

        status.Vote.Should().Be(ScaleVote.ScaleOut);
    }

    [Fact]
    public void GetScaleStatus_QueueAboveWorkersTimesTarget_VotesScaleOut()
    {
        var status = Monitor(ClientReturning(0, 0).Object, target: 10).GetScaleStatus(Context(2, 21));

        status.Vote.Should().Be(ScaleVote.ScaleOut);
    }

    [Fact]
    public void GetScaleStatus_QueueAtWorkersTimesTarget_VotesNone()
    {
        var status = Monitor(ClientReturning(0, 0).Object, target: 10).GetScaleStatus(Context(2, 20));

        status.Vote.Should().Be(ScaleVote.None);
    }

    [Fact]
    public void GetScaleStatus_EmptyForAllRecentSamples_VotesScaleIn()
    {
        var status = Monitor(ClientReturning(0, 0).Object).GetScaleStatus(Context(3, 4, 0, 0, 0, 0, 0));

        status.Vote.Should().Be(ScaleVote.ScaleIn);
    }

    [Fact]
    public void GetScaleStatus_EmptyForTooFewSamples_VotesNone()
    {
        var status = Monitor(ClientReturning(0, 0).Object).GetScaleStatus(Context(3, 0, 0, 0, 0));

        status.Vote.Should().Be(ScaleVote.None);
    }

    [Fact]
    public void GetScaleStatus_OneNonEmptySampleInWindow_VotesNone()
    {
        var status = Monitor(ClientReturning(0, 0).Object).GetScaleStatus(Context(3, 0, 0, 1, 0, 0));

        status.Vote.Should().Be(ScaleVote.None);
    }

    [Fact]
    public void Constructor_NonPositiveTarget_Throws()
    {
        var action = () => Monitor(ClientReturning(0, 0).Object, target: 0);

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region Target scaler

    [Theory]
    [InlineData(0, 0, 10, 0)]
    [InlineData(1, 0, 10, 1)]
    [InlineData(10, 0, 10, 1)]
    [InlineData(11, 0, 10, 2)]
    [InlineData(95, 5, 10, 10)]
    [InlineData(100, 1, 10, 11)]
    public async Task GetScaleResultAsync_RoundsUpQueueLengthOverTarget(int visible, int inFlight, int target, int expected)
    {
        var scaler = new SqsQueueTargetScaler(FunctionId, ClientReturning(visible, inFlight).Object, QueueUrl, target);

        var result = await scaler.GetScaleResultAsync(new TargetScalerContext());

        result.TargetWorkerCount.Should().Be(expected);
    }

    [Fact]
    public void TargetScalerDescriptor_UsesFunctionId()
    {
        var scaler = new SqsQueueTargetScaler(FunctionId, ClientReturning(0, 0).Object, QueueUrl, 10);

        scaler.TargetScalerDescriptor.FunctionId.Should().Be(FunctionId);
    }

    [Fact]
    public async Task GetScaleResultAsync_ClientFails_Rethrows()
    {
        var client = new Mock<IAmazonSQS>();
        client.Setup(c => c.GetQueueAttributesAsync(It.IsAny<GetQueueAttributesRequest>(), default))
            .ThrowsAsync(new AmazonSQSException("denied"));
        var scaler = new SqsQueueTargetScaler(FunctionId, client.Object, QueueUrl, 10);

        var action = () => scaler.GetScaleResultAsync(new TargetScalerContext());

        await action.Should().ThrowAsync<AmazonSQSException>();
    }

    #endregion

    #region Listener wiring

    private static SqsQueueTriggerListener Listener(SqsQueueOptions? options = null) =>
        new(new SqsQueueTriggerAttribute { QueueUrl = QueueUrl },
            Options.Create(options ?? new SqsQueueOptions()),
            new Mock<ITriggeredFunctionExecutor>().Object,
            functionId: FunctionId);

    [Fact]
    public void Listener_ProvidesMonitorAndTargetScalerWithSameFunctionId()
    {
        using var listener = Listener();

        var monitor = ((IScaleMonitorProvider)listener).GetMonitor();
        var scaler = ((ITargetScalerProvider)listener).GetTargetScaler();

        monitor.Descriptor.FunctionId.Should().Be(FunctionId);
        scaler.TargetScalerDescriptor.FunctionId.Should().Be(FunctionId);
    }

    [Fact]
    public void Listener_ReturnsSameInstancesOnRepeatedCalls()
    {
        using var listener = Listener();

        listener.GetMonitor().Should().BeSameAs(listener.GetMonitor());
        listener.GetTargetScaler().Should().BeSameAs(listener.GetTargetScaler());
    }

    #endregion
}
