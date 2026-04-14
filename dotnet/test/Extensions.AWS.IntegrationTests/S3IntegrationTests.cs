using Amazon.S3;
using Amazon.S3.Model;
using FluentAssertions;
using System.Text;
using Xunit;

namespace Extensions.AWS.IntegrationTests;

[Collection("LocalStack")]
public class S3IntegrationTests
{
    private readonly LocalStackFixture _fixture;
    private readonly AmazonS3Client _s3Client;

    public S3IntegrationTests(LocalStackFixture fixture)
    {
        _fixture = fixture;
        _s3Client = new AmazonS3Client(
            _fixture.AccessKey,
            _fixture.SecretKey,
            new AmazonS3Config
            {
                ServiceURL = _fixture.Endpoint,
                ForcePathStyle = true,
                AuthenticationRegion = _fixture.Region
            });
    }

    [Fact]
    public async Task CreateBucket_ShouldSucceed()
    {
        // Arrange
        var bucketName = $"test-bucket-{Guid.NewGuid():N}";

        // Act
        var response = await _s3Client.PutBucketAsync(new PutBucketRequest
        {
            BucketName = bucketName
        });

        // Assert
        response.HttpStatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListBuckets_ShouldReturnCreatedBucket()
    {
        // Arrange
        var bucketName = $"test-bucket-{Guid.NewGuid():N}";
        await _s3Client.PutBucketAsync(new PutBucketRequest
        {
            BucketName = bucketName
        });

        // Act
        var response = await _s3Client.ListBucketsAsync();

        // Assert
        response.Buckets.Should().Contain(b => b.BucketName == bucketName);
    }

    [Fact]
    public async Task PutObject_ShouldSucceed()
    {
        // Arrange
        var bucketName = $"test-bucket-{Guid.NewGuid():N}";
        await _s3Client.PutBucketAsync(new PutBucketRequest { BucketName = bucketName });

        var objectKey = "test-object.txt";
        var content = "Hello from integration test!";

        // Act
        var response = await _s3Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = bucketName,
            Key = objectKey,
            ContentBody = content
        });

        // Assert
        response.HttpStatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        response.ETag.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetObject_ShouldReturnContent()
    {
        // Arrange
        var bucketName = $"test-bucket-{Guid.NewGuid():N}";
        await _s3Client.PutBucketAsync(new PutBucketRequest { BucketName = bucketName });

        var objectKey = "test-object.txt";
        var content = "Hello from S3 integration test!";

        await _s3Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = bucketName,
            Key = objectKey,
            ContentBody = content
        });

        // Act
        var response = await _s3Client.GetObjectAsync(new GetObjectRequest
        {
            BucketName = bucketName,
            Key = objectKey
        });

        using var reader = new StreamReader(response.ResponseStream);
        var retrievedContent = await reader.ReadToEndAsync();

        // Assert
        retrievedContent.Should().Be(content);
    }

    [Fact]
    public async Task ListObjects_ShouldReturnUploadedObjects()
    {
        // Arrange
        var bucketName = $"test-bucket-{Guid.NewGuid():N}";
        await _s3Client.PutBucketAsync(new PutBucketRequest { BucketName = bucketName });

        var objectKeys = new[] { "file1.txt", "file2.txt", "file3.txt" };
        foreach (var key in objectKeys)
        {
            await _s3Client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = bucketName,
                Key = key,
                ContentBody = $"Content of {key}"
            });
        }

        // Act
        var response = await _s3Client.ListObjectsV2Async(new ListObjectsV2Request
        {
            BucketName = bucketName
        });

        // Assert
        response.S3Objects.Should().HaveCount(3);
        response.S3Objects.Select(o => o.Key).Should().BeEquivalentTo(objectKeys);
    }

    [Fact]
    public async Task DeleteObject_ShouldSucceed()
    {
        // Arrange
        var bucketName = $"test-bucket-{Guid.NewGuid():N}";
        await _s3Client.PutBucketAsync(new PutBucketRequest { BucketName = bucketName });

        var objectKey = "to-delete.txt";
        await _s3Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = bucketName,
            Key = objectKey,
            ContentBody = "This will be deleted"
        });

        // Act
        await _s3Client.DeleteObjectAsync(new DeleteObjectRequest
        {
            BucketName = bucketName,
            Key = objectKey
        });

        // Assert
        var listResponse = await _s3Client.ListObjectsV2Async(new ListObjectsV2Request
        {
            BucketName = bucketName
        });
        listResponse.S3Objects.Should().NotContain(o => o.Key == objectKey);
    }

    [Fact]
    public async Task PutObject_WithMetadata_ShouldPreserveMetadata()
    {
        // Arrange
        var bucketName = $"test-bucket-{Guid.NewGuid():N}";
        await _s3Client.PutBucketAsync(new PutBucketRequest { BucketName = bucketName });

        var objectKey = "metadata-test.txt";
        var request = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = objectKey,
            ContentBody = "Content with metadata"
        };
        request.Metadata.Add("custom-key", "custom-value");
        request.Metadata.Add("author", "integration-test");

        // Act
        await _s3Client.PutObjectAsync(request);

        var response = await _s3Client.GetObjectMetadataAsync(new GetObjectMetadataRequest
        {
            BucketName = bucketName,
            Key = objectKey
        });

        // Assert
        response.Metadata["x-amz-meta-custom-key"].Should().Be("custom-value");
        response.Metadata["x-amz-meta-author"].Should().Be("integration-test");
    }

    [Fact]
    public async Task PutObject_WithContentType_ShouldPreserveContentType()
    {
        // Arrange
        var bucketName = $"test-bucket-{Guid.NewGuid():N}";
        await _s3Client.PutBucketAsync(new PutBucketRequest { BucketName = bucketName });

        var objectKey = "data.json";
        var jsonContent = "{\"key\": \"value\"}";

        // Act
        await _s3Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = bucketName,
            Key = objectKey,
            ContentBody = jsonContent,
            ContentType = "application/json"
        });

        var response = await _s3Client.GetObjectAsync(new GetObjectRequest
        {
            BucketName = bucketName,
            Key = objectKey
        });

        // Assert
        response.Headers.ContentType.Should().Be("application/json");
    }

    [Fact]
    public async Task CopyObject_ShouldSucceed()
    {
        // Arrange
        var bucketName = $"test-bucket-{Guid.NewGuid():N}";
        await _s3Client.PutBucketAsync(new PutBucketRequest { BucketName = bucketName });

        var sourceKey = "source.txt";
        var destKey = "destination.txt";
        var content = "Content to copy";

        await _s3Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = bucketName,
            Key = sourceKey,
            ContentBody = content
        });

        // Act
        await _s3Client.CopyObjectAsync(new CopyObjectRequest
        {
            SourceBucket = bucketName,
            SourceKey = sourceKey,
            DestinationBucket = bucketName,
            DestinationKey = destKey
        });

        // Assert
        var response = await _s3Client.GetObjectAsync(new GetObjectRequest
        {
            BucketName = bucketName,
            Key = destKey
        });

        using var reader = new StreamReader(response.ResponseStream);
        var copiedContent = await reader.ReadToEndAsync();
        copiedContent.Should().Be(content);
    }

    [Fact]
    public async Task PutObject_LargeFile_ShouldSucceed()
    {
        // Arrange
        var bucketName = $"test-bucket-{Guid.NewGuid():N}";
        await _s3Client.PutBucketAsync(new PutBucketRequest { BucketName = bucketName });

        var objectKey = "large-file.bin";
        var largeContent = new byte[1024 * 1024]; // 1 MB
        new Random().NextBytes(largeContent);

        // Act
        using var stream = new MemoryStream(largeContent);
        var response = await _s3Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = bucketName,
            Key = objectKey,
            InputStream = stream
        });

        // Assert
        response.HttpStatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var getResponse = await _s3Client.GetObjectAsync(new GetObjectRequest
        {
            BucketName = bucketName,
            Key = objectKey
        });
        getResponse.ContentLength.Should().Be(1024 * 1024);
    }

    [Fact]
    public async Task ListObjects_WithPrefix_ShouldFilterResults()
    {
        // Arrange
        var bucketName = $"test-bucket-{Guid.NewGuid():N}";
        await _s3Client.PutBucketAsync(new PutBucketRequest { BucketName = bucketName });

        var objects = new[] { "images/photo1.jpg", "images/photo2.jpg", "documents/doc1.pdf", "documents/doc2.pdf" };
        foreach (var key in objects)
        {
            await _s3Client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = bucketName,
                Key = key,
                ContentBody = $"Content of {key}"
            });
        }

        // Act
        var response = await _s3Client.ListObjectsV2Async(new ListObjectsV2Request
        {
            BucketName = bucketName,
            Prefix = "images/"
        });

        // Assert
        response.S3Objects.Should().HaveCount(2);
        response.S3Objects.Should().OnlyContain(o => o.Key.StartsWith("images/"));
    }

    [Fact]
    public async Task DeleteObjects_Batch_ShouldSucceed()
    {
        // Arrange
        var bucketName = $"test-bucket-{Guid.NewGuid():N}";
        await _s3Client.PutBucketAsync(new PutBucketRequest { BucketName = bucketName });

        var objectKeys = Enumerable.Range(1, 5).Select(i => $"file{i}.txt").ToList();
        foreach (var key in objectKeys)
        {
            await _s3Client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = bucketName,
                Key = key,
                ContentBody = "Content"
            });
        }

        // Act
        var response = await _s3Client.DeleteObjectsAsync(new DeleteObjectsRequest
        {
            BucketName = bucketName,
            Objects = objectKeys.Select(k => new KeyVersion { Key = k }).ToList()
        });

        // Assert
        response.DeletedObjects.Should().HaveCount(5);

        var listResponse = await _s3Client.ListObjectsV2Async(new ListObjectsV2Request
        {
            BucketName = bucketName
        });
        listResponse.S3Objects.Should().BeEmpty();
    }
}
