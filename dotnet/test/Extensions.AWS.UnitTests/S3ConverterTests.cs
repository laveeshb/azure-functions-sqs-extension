using System.Text.Json;
using Azure.Functions.Worker.Extensions.S3;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker.Converters;
using Moq;
using Xunit;

namespace Extensions.AWS.UnitTests;

public class S3ConverterTests
{
    private readonly S3EventConverter _converter = new();

    #region S3Event Tests

    [Fact]
    public async Task ConvertAsync_WithValidS3EventJson_ReturnsS3Event()
    {
        // Arrange
        var json = """
        {
            "Records": [
                {
                    "eventVersion": "2.1",
                    "eventSource": "aws:s3",
                    "awsRegion": "us-east-1",
                    "eventTime": "2024-01-15T10:30:00Z",
                    "eventName": "ObjectCreated:Put",
                    "userIdentity": {
                        "principalId": "EXAMPLE"
                    },
                    "requestParameters": {
                        "sourceIPAddress": "127.0.0.1"
                    },
                    "responseElements": {
                        "x-amz-request-id": "EXAMPLE123456789",
                        "x-amz-id-2": "EXAMPLE123/456"
                    },
                    "s3": {
                        "s3SchemaVersion": "1.0",
                        "configurationId": "testConfigRule",
                        "bucket": {
                            "name": "my-bucket",
                            "ownerIdentity": {
                                "principalId": "EXAMPLE"
                            },
                            "arn": "arn:aws:s3:::my-bucket"
                        },
                        "object": {
                            "key": "uploads/test-file.json",
                            "size": 1024,
                            "eTag": "0123456789abcdef",
                            "versionId": "096fKKXTRTtl3on89fVO.nfljtsv6qko",
                            "sequencer": "0A1B2C3D4E5F678901"
                        }
                    }
                }
            ]
        }
        """;

        var context = CreateConverterContext(json, typeof(S3Event));

        // Act
        var result = await _converter.ConvertAsync(context);

        // Assert
        result.Status.Should().Be(ConversionStatus.Succeeded);
        result.Value.Should().BeOfType<S3Event>();
        var s3Event = (S3Event)result.Value!;
        s3Event.Records.Should().HaveCount(1);
        s3Event.Records![0].EventSource.Should().Be("aws:s3");
        s3Event.Records[0].EventName.Should().Be("ObjectCreated:Put");
        s3Event.Records[0].S3!.Bucket!.Name.Should().Be("my-bucket");
        s3Event.Records[0].S3.Object!.Key.Should().Be("uploads/test-file.json");
        s3Event.Records[0].S3.Object.Size.Should().Be(1024);
    }

    [Fact]
    public async Task ConvertAsync_WithS3EventRecordTargetType_ReturnsSingleRecord()
    {
        // Arrange
        var json = """
        {
            "Records": [
                {
                    "eventVersion": "2.1",
                    "eventSource": "aws:s3",
                    "awsRegion": "eu-west-1",
                    "eventName": "ObjectRemoved:Delete",
                    "s3": {
                        "bucket": {
                            "name": "test-bucket"
                        },
                        "object": {
                            "key": "deleted-file.txt"
                        }
                    }
                }
            ]
        }
        """;

        var context = CreateConverterContext(json, typeof(S3EventRecord));

        // Act
        var result = await _converter.ConvertAsync(context);

        // Assert
        result.Status.Should().Be(ConversionStatus.Succeeded);
        result.Value.Should().BeOfType<S3EventRecord>();
        var record = (S3EventRecord)result.Value!;
        record.EventName.Should().Be("ObjectRemoved:Delete");
        record.S3!.Bucket!.Name.Should().Be("test-bucket");
        record.S3.Object!.Key.Should().Be("deleted-file.txt");
    }

    [Fact]
    public async Task ConvertAsync_WithSqsWrappedS3Event_ReturnsS3Event()
    {
        // Arrange - S3 events come wrapped in SQS message
        var s3Json = """
        {
            "Records": [
                {
                    "eventSource": "aws:s3",
                    "eventName": "ObjectCreated:Put",
                    "s3": {
                        "bucket": { "name": "wrapped-bucket" },
                        "object": { "key": "wrapped-key.txt" }
                    }
                }
            ]
        }
        """;
        var sqsWrapper = new { Body = s3Json };
        var json = JsonSerializer.Serialize(sqsWrapper);

        var context = CreateConverterContext(json, typeof(S3Event));

        // Act
        var result = await _converter.ConvertAsync(context);

        // Assert
        result.Status.Should().Be(ConversionStatus.Succeeded);
        var s3Event = (S3Event)result.Value!;
        s3Event.Records![0].S3!.Bucket!.Name.Should().Be("wrapped-bucket");
    }

    [Fact]
    public async Task ConvertAsync_WithStringTargetType_ReturnsJsonString()
    {
        // Arrange
        var json = """{"Records":[{"eventSource":"aws:s3"}]}""";

        var context = CreateConverterContext(json, typeof(string));

        // Act
        var result = await _converter.ConvertAsync(context);

        // Assert
        result.Status.Should().Be(ConversionStatus.Succeeded);
        result.Value.Should().BeOfType<string>();
        ((string)result.Value!).Should().Contain("aws:s3");
    }

    [Fact]
    public async Task ConvertAsync_WithEmptyRecords_ThrowsForRecordType()
    {
        // Arrange
        var json = """{"Records":[]}""";
        var context = CreateConverterContext(json, typeof(S3EventRecord));

        // Act
        var result = await _converter.ConvertAsync(context);

        // Assert
        result.Status.Should().Be(ConversionStatus.Failed);
    }

    [Fact]
    public async Task ConvertAsync_WithEmptySource_ReturnsUnhandled()
    {
        // Arrange
        var context = CreateConverterContext("", typeof(S3Event));

        // Act
        var result = await _converter.ConvertAsync(context);

        // Assert
        result.Status.Should().Be(ConversionStatus.Unhandled);
    }

    [Fact]
    public async Task ConvertAsync_WithInvalidJson_ReturnsFailed()
    {
        // Arrange
        var context = CreateConverterContext("invalid json {{", typeof(S3Event));

        // Act
        var result = await _converter.ConvertAsync(context);

        // Assert
        result.Status.Should().Be(ConversionStatus.Failed);
    }

    #endregion

    #region S3 Model Tests

    [Fact]
    public void S3Event_Deserializes_MultipleRecords()
    {
        // Arrange
        var json = """
        {
            "Records": [
                {
                    "eventName": "ObjectCreated:Put",
                    "s3": { "bucket": { "name": "bucket1" }, "object": { "key": "key1" } }
                },
                {
                    "eventName": "ObjectCreated:Copy",
                    "s3": { "bucket": { "name": "bucket2" }, "object": { "key": "key2" } }
                }
            ]
        }
        """;

        // Act
        var s3Event = JsonSerializer.Deserialize<S3Event>(json);

        // Assert
        s3Event.Should().NotBeNull();
        s3Event!.Records.Should().HaveCount(2);
        s3Event.Records![0].S3!.Bucket!.Name.Should().Be("bucket1");
        s3Event.Records[1].S3!.Bucket!.Name.Should().Be("bucket2");
    }

    [Fact]
    public void S3EventRecord_Deserializes_AllProperties()
    {
        // Arrange
        var json = """
        {
            "eventVersion": "2.1",
            "eventSource": "aws:s3",
            "awsRegion": "ap-southeast-1",
            "eventTime": "2024-06-15T14:30:00Z",
            "eventName": "ObjectCreated:Put",
            "userIdentity": { "principalId": "AIDAJDPLRKLG7UEXAMPLE" },
            "requestParameters": { "sourceIPAddress": "10.0.0.1" },
            "responseElements": { "x-amz-request-id": "req123", "x-amz-id-2": "id2" },
            "s3": {
                "s3SchemaVersion": "1.0",
                "configurationId": "config1",
                "bucket": {
                    "name": "test-bucket",
                    "ownerIdentity": { "principalId": "owner123" },
                    "arn": "arn:aws:s3:::test-bucket"
                },
                "object": {
                    "key": "folder/file.txt",
                    "size": 2048,
                    "eTag": "abc123",
                    "versionId": "v1",
                    "sequencer": "seq123"
                }
            }
        }
        """;

        // Act
        var record = JsonSerializer.Deserialize<S3EventRecord>(json);

        // Assert
        record.Should().NotBeNull();
        record!.EventVersion.Should().Be("2.1");
        record.AwsRegion.Should().Be("ap-southeast-1");
        record.EventTime.Should().Be(new DateTime(2024, 6, 15, 14, 30, 0, DateTimeKind.Utc));
        record.UserIdentity!.PrincipalId.Should().Be("AIDAJDPLRKLG7UEXAMPLE");
        record.RequestParameters!.SourceIpAddress.Should().Be("10.0.0.1");
        record.ResponseElements!.RequestId.Should().Be("req123");
        record.S3!.S3SchemaVersion.Should().Be("1.0");
        record.S3.Bucket!.Arn.Should().Be("arn:aws:s3:::test-bucket");
        record.S3.Object!.Size.Should().Be(2048);
        record.S3.Object.ETag.Should().Be("abc123");
    }

    #endregion

    #region S3OutputObject Model Tests

    [Fact]
    public void S3OutputObject_Serializes_Correctly()
    {
        // Arrange
        var outputObject = new S3OutputObject
        {
            Key = "output/file.json",
            Content = "{\"result\":\"success\"}",
            ContentType = "application/json",
            Metadata = new Dictionary<string, string>
            {
                ["processed-by"] = "my-function",
                ["timestamp"] = "2024-01-15"
            }
        };

        // Act
        var json = JsonSerializer.Serialize(outputObject);
        var deserialized = JsonSerializer.Deserialize<S3OutputObject>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Key.Should().Be("output/file.json");
        deserialized.Content!.ToString().Should().Contain("success");
        deserialized.ContentType.Should().Be("application/json");
        deserialized.Metadata.Should().ContainKey("processed-by");
    }

    #endregion

    private static ConverterContext CreateConverterContext(string? source, Type targetType)
    {
        var functionContext = new Mock<Microsoft.Azure.Functions.Worker.FunctionContext>();
        return new TestConverterContext(targetType, source, functionContext.Object);
    }
}
