using System.Text.Json;
using Azure.Functions.Worker.Extensions.Kinesis;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker.Converters;
using Moq;
using Xunit;

namespace Extensions.AWS.UnitTests;

public class KinesisConverterTests
{
    private readonly KinesisRecordConverter _converter = new();

    #region KinesisRecord Tests

    [Fact]
    public async Task ConvertAsync_WithValidKinesisJson_ReturnsKinesisRecord()
    {
        // Arrange - Data is base64 encoded
        var dataContent = "Hello, Kinesis!";
        var base64Data = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(dataContent));
        var json = $$"""
        {
            "sequenceNumber": "49590338271490256608559692538361571095921575989136588802",
            "approximateArrivalTimestamp": "2024-01-15T10:30:00Z",
            "partitionKey": "partition-1",
            "data": "{{base64Data}}",
            "encryptionType": "NONE",
            "shardId": "shardId-000000000000"
        }
        """;

        var context = CreateConverterContext(json, typeof(KinesisRecord));

        // Act
        var result = await _converter.ConvertAsync(context);

        // Assert
        result.Status.Should().Be(ConversionStatus.Succeeded);
        result.Value.Should().BeOfType<KinesisRecord>();
        var record = (KinesisRecord)result.Value!;
        record.SequenceNumber.Should().StartWith("49590338271490256608559692538361571095921575989136588802");
        record.PartitionKey.Should().Be("partition-1");
        record.Data.Should().Be(base64Data);
        record.EncryptionType.Should().Be("NONE");
        record.ShardId.Should().Be("shardId-000000000000");
    }

    [Fact]
    public async Task ConvertAsync_WithStringTargetType_ReturnsDataString()
    {
        // Arrange
        var base64Data = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("Test message"));
        var json = $$"""
        {
            "sequenceNumber": "123",
            "partitionKey": "pk",
            "data": "{{base64Data}}"
        }
        """;

        var context = CreateConverterContext(json, typeof(string));

        // Act
        var result = await _converter.ConvertAsync(context);

        // Assert
        result.Status.Should().Be(ConversionStatus.Succeeded);
        result.Value.Should().Be(base64Data);
    }

    [Fact]
    public async Task ConvertAsync_WithByteArrayTargetType_ReturnsDecodedBytes()
    {
        // Arrange
        var originalContent = "Binary data content";
        var base64Data = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(originalContent));
        var json = $$"""
        {
            "sequenceNumber": "123",
            "partitionKey": "pk",
            "data": "{{base64Data}}"
        }
        """;

        var context = CreateConverterContext(json, typeof(byte[]));

        // Act
        var result = await _converter.ConvertAsync(context);

        // Assert
        result.Status.Should().Be(ConversionStatus.Succeeded);
        result.Value.Should().BeOfType<byte[]>();
        var bytes = (byte[])result.Value!;
        var decoded = System.Text.Encoding.UTF8.GetString(bytes);
        decoded.Should().Be(originalContent);
    }

    [Fact]
    public async Task ConvertAsync_WithEmptySource_ReturnsUnhandled()
    {
        // Arrange
        var context = CreateConverterContext("", typeof(KinesisRecord));

        // Act
        var result = await _converter.ConvertAsync(context);

        // Assert
        result.Status.Should().Be(ConversionStatus.Unhandled);
    }

    [Fact]
    public async Task ConvertAsync_WithInvalidJson_ReturnsFailed()
    {
        // Arrange
        var context = CreateConverterContext("not valid json", typeof(KinesisRecord));

        // Act
        var result = await _converter.ConvertAsync(context);

        // Assert
        result.Status.Should().Be(ConversionStatus.Failed);
    }

    [Fact]
    public async Task ConvertAsync_WithInvalidBase64ForByteArray_ReturnsFailed()
    {
        // Arrange - Invalid base64 data
        var json = """
        {
            "sequenceNumber": "123",
            "data": "not-valid-base64!@#$"
        }
        """;

        var context = CreateConverterContext(json, typeof(byte[]));

        // Act
        var result = await _converter.ConvertAsync(context);

        // Assert
        result.Status.Should().Be(ConversionStatus.Failed);
    }

    #endregion

    #region KinesisRecord Model Tests

    [Fact]
    public void KinesisRecord_DecodedData_ReturnsDecodedString()
    {
        // Arrange
        var originalText = "This is the original message";
        var base64Data = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(originalText));
        var json = $$"""
        {
            "sequenceNumber": "seq-123",
            "partitionKey": "pk-1",
            "data": "{{base64Data}}"
        }
        """;

        // Act
        var record = JsonSerializer.Deserialize<KinesisRecord>(json);

        // Assert
        record.Should().NotBeNull();
        record!.Data.Should().Be(base64Data);
        record.DecodedData.Should().Be(originalText);
    }

    [Fact]
    public void KinesisRecord_DecodedData_WithNullData_ReturnsNull()
    {
        // Arrange
        var record = new KinesisRecord
        {
            SequenceNumber = "123",
            PartitionKey = "pk"
        };

        // Act & Assert
        record.DecodedData.Should().BeNull();
    }

    [Fact]
    public void KinesisRecord_DecodedData_WithInvalidBase64_ReturnsOriginal()
    {
        // Arrange - If base64 decode fails, returns original data
        var record = new KinesisRecord
        {
            SequenceNumber = "123",
            Data = "not-base64"
        };

        // Act & Assert
        record.DecodedData.Should().Be("not-base64");
    }

    [Fact]
    public void KinesisRecord_Deserializes_AllProperties()
    {
        // Arrange
        var json = """
        {
            "sequenceNumber": "49590338271490256608559692538361571095921575989136588802",
            "approximateArrivalTimestamp": "2024-06-15T14:30:00Z",
            "partitionKey": "user-123",
            "data": "SGVsbG8gV29ybGQ=",
            "encryptionType": "KMS",
            "shardId": "shardId-000000000001"
        }
        """;

        // Act
        var record = JsonSerializer.Deserialize<KinesisRecord>(json);

        // Assert
        record.Should().NotBeNull();
        record!.SequenceNumber.Should().StartWith("495903382714");
        record.ApproximateArrivalTimestamp.Should().Be(new DateTime(2024, 6, 15, 14, 30, 0, DateTimeKind.Utc));
        record.PartitionKey.Should().Be("user-123");
        record.EncryptionType.Should().Be("KMS");
        record.ShardId.Should().Be("shardId-000000000001");
        record.DecodedData.Should().Be("Hello World");
    }

    #endregion

    #region KinesisOutputRecord Model Tests

    [Fact]
    public void KinesisOutputRecord_Serializes_Correctly()
    {
        // Arrange
        var outputRecord = new KinesisOutputRecord
        {
            Data = "Output data content",
            PartitionKey = "output-partition",
            ExplicitHashKey = "hash-key-123"
        };

        // Act
        var json = JsonSerializer.Serialize(outputRecord);
        var deserialized = JsonSerializer.Deserialize<KinesisOutputRecord>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Data!.ToString().Should().Be("Output data content");
        deserialized.PartitionKey.Should().Be("output-partition");
        deserialized.ExplicitHashKey.Should().Be("hash-key-123");
    }

    [Fact]
    public void KinesisOutputRecord_WithBinaryData_SerializesCorrectly()
    {
        // Arrange
        var binaryData = System.Text.Encoding.UTF8.GetBytes("Binary payload");
        var base64 = Convert.ToBase64String(binaryData);
        var outputRecord = new KinesisOutputRecord
        {
            Data = base64,
            PartitionKey = "binary-partition"
        };

        // Act
        var json = JsonSerializer.Serialize(outputRecord);

        // Assert
        json.Should().Contain(base64);
        json.Should().Contain("binary-partition");
    }

    #endregion

    private static ConverterContext CreateConverterContext(string? source, Type targetType)
    {
        var functionContext = new Mock<Microsoft.Azure.Functions.Worker.FunctionContext>();
        return new TestConverterContext(targetType, source, functionContext.Object);
    }
}
