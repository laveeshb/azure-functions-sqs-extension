using Azure.WebJobs.Extensions.Kinesis;
using Xunit;
using FluentAssertions;
using System.Text;
using System.Text.Json;

namespace Extensions.AWS.UnitTests;

public class KinesisTriggerTests
{
    private const string ValidStreamName = "test-stream";

    #region KinesisTriggerAttribute Tests

    [Fact]
    public void KinesisTriggerAttribute_Constructor_SetsStreamName()
    {
        var attribute = new KinesisTriggerAttribute(ValidStreamName);

        attribute.StreamName.Should().Be(ValidStreamName);
    }

    [Fact]
    public void KinesisTriggerAttribute_Constructor_WithNull_ThrowsArgumentNullException()
    {
        var action = () => new KinesisTriggerAttribute(null!);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("streamName");
    }

    [Fact]
    public void KinesisTriggerAttribute_DefaultValues()
    {
        var attribute = new KinesisTriggerAttribute(ValidStreamName);

        attribute.BatchSize.Should().Be(100);
        attribute.PollingIntervalMs.Should().Be(1000);
        attribute.StartingPosition.Should().Be("TRIM_HORIZON");
    }

    [Fact]
    public void KinesisTriggerAttribute_SetsAllProperties()
    {
        var attribute = new KinesisTriggerAttribute(ValidStreamName)
        {
            AWSKeyId = "test-key",
            AWSAccessKey = "test-secret",
            Region = "us-west-2",
            BatchSize = 50,
            PollingIntervalMs = 2000,
            StartingPosition = "LATEST"
        };

        attribute.StreamName.Should().Be(ValidStreamName);
        attribute.AWSKeyId.Should().Be("test-key");
        attribute.AWSAccessKey.Should().Be("test-secret");
        attribute.Region.Should().Be("us-west-2");
        attribute.BatchSize.Should().Be(50);
        attribute.PollingIntervalMs.Should().Be(2000);
        attribute.StartingPosition.Should().Be("LATEST");
    }

    [Fact]
    public void KinesisTriggerAttribute_SupportsAppSettingsPattern()
    {
        var attribute = new KinesisTriggerAttribute("%KINESIS_STREAM_NAME%");

        attribute.StreamName.Should().Be("%KINESIS_STREAM_NAME%");
    }

    #endregion

    #region KinesisRecord Tests

    [Fact]
    public void KinesisRecord_DataAsString_ReturnsUtf8DecodedData()
    {
        var testData = "Hello, Kinesis!";
        var record = new KinesisRecord
        {
            SequenceNumber = "seq-123",
            PartitionKey = "pk-1",
            ShardId = "shard-001",
            DataBytes = Encoding.UTF8.GetBytes(testData)
        };

        record.DataAsString.Should().Be(testData);
    }

    [Fact]
    public void KinesisRecord_DataAsString_WithNullBytes_ReturnsNull()
    {
        var record = new KinesisRecord
        {
            SequenceNumber = "seq-123",
            PartitionKey = "pk-1",
            DataBytes = null
        };

        record.DataAsString.Should().BeNull();
    }

    [Fact]
    public void KinesisRecord_DataAsString_WithEmptyBytes_ReturnsEmptyString()
    {
        var record = new KinesisRecord
        {
            SequenceNumber = "seq-123",
            PartitionKey = "pk-1",
            DataBytes = Array.Empty<byte>()
        };

        record.DataAsString.Should().BeEmpty();
    }

    [Fact]
    public void KinesisRecord_GetData_DeserializesJsonPayload()
    {
        var testEvent = new TestEvent { Id = "123", Name = "Test Event" };
        var json = JsonSerializer.Serialize(testEvent);
        
        var record = new KinesisRecord
        {
            SequenceNumber = "seq-123",
            PartitionKey = "pk-1",
            DataBytes = Encoding.UTF8.GetBytes(json)
        };

        var result = record.GetData<TestEvent>();

        result.Should().NotBeNull();
        result!.Id.Should().Be("123");
        result.Name.Should().Be("Test Event");
    }

    [Fact]
    public void KinesisRecord_GetData_WithNullBytes_ReturnsDefault()
    {
        var record = new KinesisRecord
        {
            SequenceNumber = "seq-123",
            PartitionKey = "pk-1",
            DataBytes = null
        };

        var result = record.GetData<TestEvent>();

        result.Should().BeNull();
    }

    [Fact]
    public void KinesisRecord_Properties_AreSetCorrectly()
    {
        var arrivalTime = DateTime.UtcNow;
        var record = new KinesisRecord
        {
            SequenceNumber = "seq-12345",
            PartitionKey = "partition-key-1",
            ShardId = "shardId-000000000001",
            ApproximateArrivalTimestamp = arrivalTime,
            EncryptionType = "KMS"
        };

        record.SequenceNumber.Should().Be("seq-12345");
        record.PartitionKey.Should().Be("partition-key-1");
        record.ShardId.Should().Be("shardId-000000000001");
        record.ApproximateArrivalTimestamp.Should().Be(arrivalTime);
        record.EncryptionType.Should().Be("KMS");
    }

    #endregion

    #region Starting Position Tests

    [Theory]
    [InlineData("TRIM_HORIZON")]
    [InlineData("LATEST")]
    [InlineData("AT_TIMESTAMP")]
    public void KinesisTriggerAttribute_AcceptsValidStartingPositions(string startingPosition)
    {
        var attribute = new KinesisTriggerAttribute(ValidStreamName)
        {
            StartingPosition = startingPosition
        };

        attribute.StartingPosition.Should().Be(startingPosition);
    }

    #endregion

    private class TestEvent
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
    }
}
