namespace Extensions.SQS.UnitTests;

using System;
using Azure.WebJobs.Extensions.SQS;
using FluentAssertions;
using Xunit;

public class AmazonSQSClientFactoryTests
{
    private const string ValidQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/my-queue";
    private const string ValidQueueUrlEuWest = "https://sqs.eu-west-1.amazonaws.com/123456789012/my-queue";
    
    #region ExtractRegionFromQueueUrl Tests

    [Theory]
    [InlineData("https://sqs.us-east-1.amazonaws.com/123456789012/my-queue", "us-east-1")]
    [InlineData("https://sqs.eu-west-1.amazonaws.com/123456789012/my-queue", "eu-west-1")]
    [InlineData("https://sqs.ap-southeast-2.amazonaws.com/123456789012/my-queue", "ap-southeast-2")]
    [InlineData("https://sqs.us-gov-west-1.amazonaws.com/123456789012/my-queue", "us-gov-west-1")]
    [InlineData("https://sqs.cn-north-1.amazonaws.com.cn/123456789012/my-queue", "cn-north-1")]
    public void Build_WithValidQueueUrl_ExtractsCorrectRegion(string queueUrl, string expectedRegion)
    {
        // Arrange
        var attribute = new SqsQueueTriggerAttribute { QueueUrl = queueUrl };

        // Act
        var client = AmazonSQSClientFactory.Build(attribute);

        // Assert
        client.Should().NotBeNull();
        client.Config.RegionEndpoint.SystemName.Should().Be(expectedRegion);
        
        // Cleanup
        client.Dispose();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-url")]
    [InlineData("https://example.com/queue")]
    public void Build_WithInvalidQueueUrl_ThrowsArgumentException(string invalidQueueUrl)
    {
        // Arrange
        var attribute = new SqsQueueTriggerAttribute { QueueUrl = invalidQueueUrl };

        // Act & Assert
        var action = () => AmazonSQSClientFactory.Build(attribute);
        action.Should().Throw<Exception>(); // Could be ArgumentException or UriFormatException
    }

    #endregion

    #region Region Override Tests

    [Fact]
    public void Build_WithRegionOverride_UsesOverrideInsteadOfQueueUrlRegion()
    {
        // Arrange
        var attribute = new SqsQueueTriggerAttribute 
        { 
            QueueUrl = ValidQueueUrl, // us-east-1 in URL
            Region = "eu-west-2"       // Override to eu-west-2
        };

        // Act
        var client = AmazonSQSClientFactory.Build(attribute);

        // Assert
        client.Should().NotBeNull();
        client.Config.RegionEndpoint.SystemName.Should().Be("eu-west-2");
        
        // Cleanup
        client.Dispose();
    }

    [Fact]
    public void Build_WithNullRegionOverride_UsesQueueUrlRegion()
    {
        // Arrange
        var attribute = new SqsQueueTriggerAttribute 
        { 
            QueueUrl = ValidQueueUrlEuWest,
            Region = null
        };

        // Act
        var client = AmazonSQSClientFactory.Build(attribute);

        // Assert
        client.Should().NotBeNull();
        client.Config.RegionEndpoint.SystemName.Should().Be("eu-west-1");
        
        // Cleanup
        client.Dispose();
    }

    [Fact]
    public void Build_WithEmptyRegionOverride_UsesQueueUrlRegion()
    {
        // Arrange
        var attribute = new SqsQueueTriggerAttribute 
        { 
            QueueUrl = ValidQueueUrlEuWest,
            Region = ""
        };

        // Act
        var client = AmazonSQSClientFactory.Build(attribute);

        // Assert
        client.Should().NotBeNull();
        client.Config.RegionEndpoint.SystemName.Should().Be("eu-west-1");
        
        // Cleanup
        client.Dispose();
    }

    #endregion

    #region Credentials Tests

    [Fact]
    public void Build_WithExplicitCredentials_CreatesClientWithCredentials()
    {
        // Arrange
        var attribute = new SqsQueueTriggerAttribute 
        { 
            QueueUrl = ValidQueueUrl,
            AWSKeyId = "AKIAIOSFODNN7EXAMPLE",
            AWSAccessKey = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY"
        };

        // Act
        var client = AmazonSQSClientFactory.Build(attribute);

        // Assert
        client.Should().NotBeNull();
        // Note: We can't directly verify credentials, but client creation should succeed
        
        // Cleanup
        client.Dispose();
    }

    [Fact]
    public void Build_WithOnlyKeyId_UsesCredentialChain()
    {
        // Arrange - Only key ID, no secret
        var attribute = new SqsQueueTriggerAttribute 
        { 
            QueueUrl = ValidQueueUrl,
            AWSKeyId = "AKIAIOSFODNN7EXAMPLE",
            AWSAccessKey = null
        };

        // Act
        var client = AmazonSQSClientFactory.Build(attribute);

        // Assert
        client.Should().NotBeNull();
        
        // Cleanup
        client.Dispose();
    }

    [Fact]
    public void Build_WithNoCredentials_UsesCredentialChain()
    {
        // Arrange
        var attribute = new SqsQueueTriggerAttribute 
        { 
            QueueUrl = ValidQueueUrl,
            AWSKeyId = null,
            AWSAccessKey = null
        };

        // Act
        var client = AmazonSQSClientFactory.Build(attribute);

        // Assert
        client.Should().NotBeNull();
        
        // Cleanup
        client.Dispose();
    }

    #endregion

    #region SqsQueueOutAttribute Tests

    [Fact]
    public void Build_WithSqsQueueOutAttribute_CreatesClient()
    {
        // Arrange
        var attribute = new SqsQueueOutAttribute 
        { 
            QueueUrl = ValidQueueUrl 
        };

        // Act
        var client = AmazonSQSClientFactory.Build(attribute);

        // Assert
        client.Should().NotBeNull();
        client.Config.RegionEndpoint.SystemName.Should().Be("us-east-1");
        
        // Cleanup
        client.Dispose();
    }

    [Fact]
    public void Build_WithSqsQueueOutAttribute_WithRegionOverride_UsesOverride()
    {
        // Arrange
        var attribute = new SqsQueueOutAttribute 
        { 
            QueueUrl = ValidQueueUrl,
            Region = "ap-northeast-1"
        };

        // Act
        var client = AmazonSQSClientFactory.Build(attribute);

        // Assert
        client.Should().NotBeNull();
        client.Config.RegionEndpoint.SystemName.Should().Be("ap-northeast-1");
        
        // Cleanup
        client.Dispose();
    }

    #endregion
}
