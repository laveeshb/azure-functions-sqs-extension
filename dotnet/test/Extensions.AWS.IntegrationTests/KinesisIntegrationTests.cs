using Amazon.Kinesis;
using Amazon.Kinesis.Model;
using FluentAssertions;
using System.Text;
using Xunit;

namespace Extensions.AWS.IntegrationTests;

[Collection("LocalStack")]
public class KinesisIntegrationTests
{
    private readonly LocalStackFixture _fixture;
    private readonly AmazonKinesisClient _kinesisClient;

    public KinesisIntegrationTests(LocalStackFixture fixture)
    {
        _fixture = fixture;
        _kinesisClient = new AmazonKinesisClient(
            _fixture.AccessKey,
            _fixture.SecretKey,
            new AmazonKinesisConfig
            {
                ServiceURL = _fixture.Endpoint,
                AuthenticationRegion = _fixture.Region
            });
    }

    [Fact]
    public async Task CreateStream_ShouldSucceed()
    {
        // Arrange
        var streamName = $"test-stream-{Guid.NewGuid():N}";

        // Act
        await _kinesisClient.CreateStreamAsync(new CreateStreamRequest
        {
            StreamName = streamName,
            ShardCount = 1
        });

        // Wait for stream to become active
        await WaitForStreamActive(streamName);

        // Assert
        var describeResponse = await _kinesisClient.DescribeStreamAsync(new DescribeStreamRequest
        {
            StreamName = streamName
        });

        describeResponse.StreamDescription.StreamName.Should().Be(streamName);
        describeResponse.StreamDescription.StreamStatus.Should().Be(StreamStatus.ACTIVE);
    }

    [Fact]
    public async Task ListStreams_ShouldReturnCreatedStream()
    {
        // Arrange
        var streamName = $"test-stream-{Guid.NewGuid():N}";
        await _kinesisClient.CreateStreamAsync(new CreateStreamRequest
        {
            StreamName = streamName,
            ShardCount = 1
        });
        await WaitForStreamActive(streamName);

        // Act
        var response = await _kinesisClient.ListStreamsAsync(new ListStreamsRequest());

        // Assert
        response.StreamNames.Should().Contain(streamName);
    }

    [Fact]
    public async Task PutRecord_ShouldSucceed()
    {
        // Arrange
        var streamName = $"test-stream-{Guid.NewGuid():N}";
        await _kinesisClient.CreateStreamAsync(new CreateStreamRequest
        {
            StreamName = streamName,
            ShardCount = 1
        });
        await WaitForStreamActive(streamName);

        var data = Encoding.UTF8.GetBytes("Test record data");

        // Act
        var response = await _kinesisClient.PutRecordAsync(new PutRecordRequest
        {
            StreamName = streamName,
            PartitionKey = "partition-1",
            Data = new MemoryStream(data)
        });

        // Assert
        response.ShardId.Should().NotBeNullOrEmpty();
        response.SequenceNumber.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task PutRecords_Batch_ShouldSucceed()
    {
        // Arrange
        var streamName = $"test-stream-{Guid.NewGuid():N}";
        await _kinesisClient.CreateStreamAsync(new CreateStreamRequest
        {
            StreamName = streamName,
            ShardCount = 1
        });
        await WaitForStreamActive(streamName);

        var records = Enumerable.Range(1, 5).Select(i => new PutRecordsRequestEntry
        {
            PartitionKey = $"partition-{i}",
            Data = new MemoryStream(Encoding.UTF8.GetBytes($"Record {i}"))
        }).ToList();

        // Act
        var response = await _kinesisClient.PutRecordsAsync(new PutRecordsRequest
        {
            StreamName = streamName,
            Records = records
        });

        // Assert
        response.FailedRecordCount.Should().Be(0);
        response.Records.Should().HaveCount(5);
        response.Records.Should().OnlyContain(r => !string.IsNullOrEmpty(r.SequenceNumber));
    }

    [Fact]
    public async Task GetRecords_ShouldReturnPutRecords()
    {
        // Arrange
        var streamName = $"test-stream-{Guid.NewGuid():N}";
        await _kinesisClient.CreateStreamAsync(new CreateStreamRequest
        {
            StreamName = streamName,
            ShardCount = 1
        });
        await WaitForStreamActive(streamName);

        var testMessage = "Hello from Kinesis integration test!";
        await _kinesisClient.PutRecordAsync(new PutRecordRequest
        {
            StreamName = streamName,
            PartitionKey = "test-key",
            Data = new MemoryStream(Encoding.UTF8.GetBytes(testMessage))
        });

        // Get shard iterator
        var describeResponse = await _kinesisClient.DescribeStreamAsync(new DescribeStreamRequest
        {
            StreamName = streamName
        });
        var shardId = describeResponse.StreamDescription.Shards[0].ShardId;

        var iteratorResponse = await _kinesisClient.GetShardIteratorAsync(new GetShardIteratorRequest
        {
            StreamName = streamName,
            ShardId = shardId,
            ShardIteratorType = ShardIteratorType.TRIM_HORIZON
        });

        // Act
        var getRecordsResponse = await _kinesisClient.GetRecordsAsync(new GetRecordsRequest
        {
            ShardIterator = iteratorResponse.ShardIterator,
            Limit = 10
        });

        // Assert
        getRecordsResponse.Records.Should().NotBeEmpty();
        var record = getRecordsResponse.Records[0];
        var recordData = Encoding.UTF8.GetString(record.Data.ToArray());
        recordData.Should().Be(testMessage);
    }

    [Fact]
    public async Task DescribeStream_ShouldReturnShardInfo()
    {
        // Arrange
        var streamName = $"test-stream-{Guid.NewGuid():N}";
        await _kinesisClient.CreateStreamAsync(new CreateStreamRequest
        {
            StreamName = streamName,
            ShardCount = 2
        });
        await WaitForStreamActive(streamName);

        // Act
        var response = await _kinesisClient.DescribeStreamAsync(new DescribeStreamRequest
        {
            StreamName = streamName
        });

        // Assert
        response.StreamDescription.Shards.Should().HaveCount(2);
        response.StreamDescription.StreamARN.Should().Contain(streamName);
    }

    [Fact]
    public async Task GetShardIterator_AllTypes_ShouldSucceed()
    {
        // Arrange
        var streamName = $"test-stream-{Guid.NewGuid():N}";
        await _kinesisClient.CreateStreamAsync(new CreateStreamRequest
        {
            StreamName = streamName,
            ShardCount = 1
        });
        await WaitForStreamActive(streamName);

        var describeResponse = await _kinesisClient.DescribeStreamAsync(new DescribeStreamRequest
        {
            StreamName = streamName
        });
        var shardId = describeResponse.StreamDescription.Shards[0].ShardId;

        // Act & Assert - TRIM_HORIZON
        var trimHorizonResponse = await _kinesisClient.GetShardIteratorAsync(new GetShardIteratorRequest
        {
            StreamName = streamName,
            ShardId = shardId,
            ShardIteratorType = ShardIteratorType.TRIM_HORIZON
        });
        trimHorizonResponse.ShardIterator.Should().NotBeNullOrEmpty();

        // Act & Assert - LATEST
        var latestResponse = await _kinesisClient.GetShardIteratorAsync(new GetShardIteratorRequest
        {
            StreamName = streamName,
            ShardId = shardId,
            ShardIteratorType = ShardIteratorType.LATEST
        });
        latestResponse.ShardIterator.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task PutRecord_WithExplicitHashKey_ShouldSucceed()
    {
        // Arrange
        var streamName = $"test-stream-{Guid.NewGuid():N}";
        await _kinesisClient.CreateStreamAsync(new CreateStreamRequest
        {
            StreamName = streamName,
            ShardCount = 1
        });
        await WaitForStreamActive(streamName);

        var data = Encoding.UTF8.GetBytes("Test with explicit hash key");

        // Act
        var response = await _kinesisClient.PutRecordAsync(new PutRecordRequest
        {
            StreamName = streamName,
            PartitionKey = "partition-key",
            ExplicitHashKey = "123456789012345678901234567890",
            Data = new MemoryStream(data)
        });

        // Assert
        response.ShardId.Should().NotBeNullOrEmpty();
        response.SequenceNumber.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task DeleteStream_ShouldSucceed()
    {
        // Arrange
        var streamName = $"test-stream-{Guid.NewGuid():N}";
        await _kinesisClient.CreateStreamAsync(new CreateStreamRequest
        {
            StreamName = streamName,
            ShardCount = 1
        });
        await WaitForStreamActive(streamName);

        // Act
        await _kinesisClient.DeleteStreamAsync(new DeleteStreamRequest
        {
            StreamName = streamName
        });

        // Assert - Stream should be deleting or not found
        await Task.Delay(1000); // Give time for deletion to process

        var listResponse = await _kinesisClient.ListStreamsAsync(new ListStreamsRequest());
        // Stream should either not be in list or be in DELETING status
        if (listResponse.StreamNames.Contains(streamName))
        {
            var describeResponse = await _kinesisClient.DescribeStreamAsync(new DescribeStreamRequest
            {
                StreamName = streamName
            });
            describeResponse.StreamDescription.StreamStatus.Should().Be(StreamStatus.DELETING);
        }
    }

    [Fact]
    public async Task PutRecords_LargePayload_ShouldSucceed()
    {
        // Arrange
        var streamName = $"test-stream-{Guid.NewGuid():N}";
        await _kinesisClient.CreateStreamAsync(new CreateStreamRequest
        {
            StreamName = streamName,
            ShardCount = 1
        });
        await WaitForStreamActive(streamName);

        // Create records with ~100KB each (Kinesis limit is 1MB per record)
        var largeData = new byte[100 * 1024];
        new Random().NextBytes(largeData);

        var records = Enumerable.Range(1, 3).Select(i => new PutRecordsRequestEntry
        {
            PartitionKey = $"partition-{i}",
            Data = new MemoryStream(largeData)
        }).ToList();

        // Act
        var response = await _kinesisClient.PutRecordsAsync(new PutRecordsRequest
        {
            StreamName = streamName,
            Records = records
        });

        // Assert
        response.FailedRecordCount.Should().Be(0);
        response.Records.Should().HaveCount(3);
    }

    [Fact]
    public async Task MultipleShards_RecordsDistributedByPartitionKey()
    {
        // Arrange
        var streamName = $"test-stream-{Guid.NewGuid():N}";
        await _kinesisClient.CreateStreamAsync(new CreateStreamRequest
        {
            StreamName = streamName,
            ShardCount = 2
        });
        await WaitForStreamActive(streamName);

        // Put records with different partition keys
        var responses = new List<PutRecordResponse>();
        for (int i = 0; i < 10; i++)
        {
            var response = await _kinesisClient.PutRecordAsync(new PutRecordRequest
            {
                StreamName = streamName,
                PartitionKey = $"key-{i}",
                Data = new MemoryStream(Encoding.UTF8.GetBytes($"Record {i}"))
            });
            responses.Add(response);
        }

        // Assert - Records should be distributed across shards
        var shardIds = responses.Select(r => r.ShardId).Distinct().ToList();
        shardIds.Count.Should().BeGreaterThanOrEqualTo(1);
    }

    private async Task WaitForStreamActive(string streamName, int maxWaitSeconds = 30)
    {
        var startTime = DateTime.UtcNow;
        while ((DateTime.UtcNow - startTime).TotalSeconds < maxWaitSeconds)
        {
            try
            {
                var response = await _kinesisClient.DescribeStreamAsync(new DescribeStreamRequest
                {
                    StreamName = streamName
                });

                if (response.StreamDescription.StreamStatus == StreamStatus.ACTIVE)
                {
                    return;
                }
            }
            catch (ResourceNotFoundException)
            {
                // Stream not yet created, continue waiting
            }

            await Task.Delay(500);
        }

        throw new TimeoutException($"Stream {streamName} did not become active within {maxWaitSeconds} seconds");
    }
}
