using Azure.WebJobs.Extensions.S3;
using Xunit;
using FluentAssertions;
using System.Text;

namespace Extensions.AWS.UnitTests;

public class S3InputBindingTests
{
    private const string ValidBucketName = "test-bucket";
    private const string ValidKey = "test-key.txt";

    #region S3Attribute Tests

    [Fact]
    public void S3Attribute_DefaultConstructor_HasNullValues()
    {
        var attribute = new S3Attribute();

        attribute.BucketName.Should().BeNull();
        attribute.Key.Should().BeNull();
    }

    [Fact]
    public void S3Attribute_ParameterizedConstructor_SetsValues()
    {
        var attribute = new S3Attribute(ValidBucketName, ValidKey);

        attribute.BucketName.Should().Be(ValidBucketName);
        attribute.Key.Should().Be(ValidKey);
    }

    [Fact]
    public void S3Attribute_SetsAllProperties()
    {
        var attribute = new S3Attribute
        {
            BucketName = ValidBucketName,
            Key = ValidKey,
            AWSKeyId = "test-key-id",
            AWSAccessKey = "test-access-key",
            Region = "us-west-2"
        };

        attribute.BucketName.Should().Be(ValidBucketName);
        attribute.Key.Should().Be(ValidKey);
        attribute.AWSKeyId.Should().Be("test-key-id");
        attribute.AWSAccessKey.Should().Be("test-access-key");
        attribute.Region.Should().Be("us-west-2");
    }

    [Fact]
    public void S3Attribute_SupportsAppSettingsPattern()
    {
        var attribute = new S3Attribute
        {
            BucketName = "%S3_BUCKET_NAME%",
            Key = "%S3_KEY%"
        };

        attribute.BucketName.Should().Be("%S3_BUCKET_NAME%");
        attribute.Key.Should().Be("%S3_KEY%");
    }

    [Fact]
    public void S3Attribute_SupportsBindingExpressions()
    {
        var attribute = new S3Attribute
        {
            BucketName = "my-bucket",
            Key = "documents/{documentId}.json"
        };

        attribute.Key.Should().Be("documents/{documentId}.json");
    }

    #endregion

    #region S3Object Tests

    [Fact]
    public void S3Object_GetContentAsString_ReturnsUtf8DecodedContent()
    {
        var textContent = "Hello, S3!";
        
        var s3Object = new S3Object
        {
            Key = ValidKey,
            BucketName = ValidBucketName,
            Content = Encoding.UTF8.GetBytes(textContent),
            ContentType = "text/plain"
        };

        var result = s3Object.GetContentAsString();

        result.Should().Be(textContent);
    }

    [Fact]
    public void S3Object_Content_StoresBinaryData()
    {
        var content = new byte[] { 1, 2, 3, 4, 5 };
        
        var s3Object = new S3Object
        {
            Key = ValidKey,
            BucketName = ValidBucketName,
            Content = content
        };

        s3Object.Content.Should().BeEquivalentTo(content);
    }

    [Fact]
    public void S3Object_Properties_AreSetCorrectly()
    {
        var lastModified = DateTime.UtcNow;
        var metadata = new Dictionary<string, string>
        {
            ["custom-key"] = "custom-value"
        };

        var s3Object = new S3Object
        {
            Key = ValidKey,
            BucketName = ValidBucketName,
            ContentType = "application/json",
            ContentLength = 1024,
            ETag = "\"abc123\"",
            LastModified = lastModified,
            Metadata = metadata
        };

        s3Object.Key.Should().Be(ValidKey);
        s3Object.BucketName.Should().Be(ValidBucketName);
        s3Object.ContentType.Should().Be("application/json");
        s3Object.ContentLength.Should().Be(1024);
        s3Object.ETag.Should().Be("\"abc123\"");
        s3Object.LastModified.Should().Be(lastModified);
        s3Object.Metadata.Should().ContainKey("custom-key");
        s3Object.Metadata["custom-key"].Should().Be("custom-value");
    }

    [Fact]
    public void S3Object_GetContentAsString_WithNullContent_ReturnsNull()
    {
        var s3Object = new S3Object
        {
            Key = ValidKey,
            BucketName = ValidBucketName,
            Content = null
        };

        var result = s3Object.GetContentAsString();

        result.Should().BeNull();
    }

    [Fact]
    public void S3Object_VersionId_CanBeSet()
    {
        var s3Object = new S3Object
        {
            Key = ValidKey,
            BucketName = ValidBucketName,
            VersionId = "abc123xyz"
        };

        s3Object.VersionId.Should().Be("abc123xyz");
    }

    #endregion

    #region S3OutAttribute Tests

    [Fact]
    public void S3OutAttribute_DefaultConstructor_HasNullValues()
    {
        var attribute = new S3OutAttribute();

        attribute.BucketName.Should().BeNull();
    }

    [Fact]
    public void S3OutAttribute_SetsAllProperties()
    {
        var attribute = new S3OutAttribute
        {
            BucketName = ValidBucketName,
            AWSKeyId = "test-key-id",
            AWSAccessKey = "test-access-key",
            Region = "eu-west-1"
        };

        attribute.BucketName.Should().Be(ValidBucketName);
        attribute.AWSKeyId.Should().Be("test-key-id");
        attribute.AWSAccessKey.Should().Be("test-access-key");
        attribute.Region.Should().Be("eu-west-1");
    }

    #endregion

    #region S3Message Tests

    [Fact]
    public void S3Message_SetsAllProperties()
    {
        var metadata = new Dictionary<string, string>
        {
            ["author"] = "test-user"
        };

        var message = new S3Message
        {
            Key = ValidKey,
            Content = "Hello, S3!",
            ContentType = "text/plain",
            ContentBytes = null,
            Metadata = metadata
        };

        message.Key.Should().Be(ValidKey);
        message.Content.Should().Be("Hello, S3!");
        message.ContentType.Should().Be("text/plain");
        message.Metadata.Should().ContainKey("author");
    }

    [Fact]
    public void S3Message_ContentBytes_TakesPrecedenceOverContent()
    {
        var bytes = new byte[] { 1, 2, 3 };
        
        var message = new S3Message
        {
            Key = ValidKey,
            Content = "text content",
            ContentBytes = bytes
        };

        message.ContentBytes.Should().NotBeNull();
        message.ContentBytes.Should().BeEquivalentTo(bytes);
    }

    #endregion
}
