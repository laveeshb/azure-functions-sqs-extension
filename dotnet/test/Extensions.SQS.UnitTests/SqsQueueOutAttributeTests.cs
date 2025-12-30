namespace Extensions.SQS.UnitTests;

using Azure.WebJobs.Extensions.SQS;
using FluentAssertions;
using Xunit;

public class SqsQueueOutAttributeTests
{
    #region Default Values Tests

    [Fact]
    public void QueueUrl_DefaultValue_IsEmptyString()
    {
        // Arrange & Act
        var attribute = new SqsQueueOutAttribute();

        // Assert
        attribute.QueueUrl.Should().Be(string.Empty);
    }

    [Fact]
    public void AWSKeyId_DefaultValue_IsNull()
    {
        // Arrange & Act
        var attribute = new SqsQueueOutAttribute();

        // Assert
        attribute.AWSKeyId.Should().BeNull();
    }

    [Fact]
    public void AWSAccessKey_DefaultValue_IsNull()
    {
        // Arrange & Act
        var attribute = new SqsQueueOutAttribute();

        // Assert
        attribute.AWSAccessKey.Should().BeNull();
    }

    [Fact]
    public void Region_DefaultValue_IsNull()
    {
        // Arrange & Act
        var attribute = new SqsQueueOutAttribute();

        // Assert
        attribute.Region.Should().BeNull();
    }

    #endregion

    #region Property Assignment Tests

    [Fact]
    public void QueueUrl_CanBeSet()
    {
        // Arrange
        var attribute = new SqsQueueOutAttribute();
        const string queueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/my-queue";

        // Act
        attribute.QueueUrl = queueUrl;

        // Assert
        attribute.QueueUrl.Should().Be(queueUrl);
    }

    [Fact]
    public void AWSKeyId_CanBeSet()
    {
        // Arrange
        var attribute = new SqsQueueOutAttribute();
        const string keyId = "AKIAIOSFODNN7EXAMPLE";

        // Act
        attribute.AWSKeyId = keyId;

        // Assert
        attribute.AWSKeyId.Should().Be(keyId);
    }

    [Fact]
    public void AWSAccessKey_CanBeSet()
    {
        // Arrange
        var attribute = new SqsQueueOutAttribute();
        const string accessKey = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY";

        // Act
        attribute.AWSAccessKey = accessKey;

        // Assert
        attribute.AWSAccessKey.Should().Be(accessKey);
    }

    [Fact]
    public void Region_CanBeSet()
    {
        // Arrange
        var attribute = new SqsQueueOutAttribute();
        const string region = "eu-west-1";

        // Act
        attribute.Region = region;

        // Assert
        attribute.Region.Should().Be(region);
    }

    #endregion

    #region Attribute Metadata Tests

    [Fact]
    public void HasBindingAttribute()
    {
        // Arrange & Act
        var attributes = typeof(SqsQueueOutAttribute).GetCustomAttributes(
            typeof(Microsoft.Azure.WebJobs.Description.BindingAttribute), 
            false);

        // Assert
        attributes.Should().HaveCount(1);
    }

    [Fact]
    public void HasAttributeUsageForParameters()
    {
        // Arrange & Act
        var attributes = typeof(SqsQueueOutAttribute).GetCustomAttributes(
            typeof(System.AttributeUsageAttribute), 
            false);

        // Assert
        attributes.Should().HaveCount(1);
        var usage = (System.AttributeUsageAttribute)attributes[0];
        usage.ValidOn.Should().Be(System.AttributeTargets.Parameter);
    }

    [Fact]
    public void QueueUrl_HasAutoResolveAttribute()
    {
        // Arrange & Act
        var property = typeof(SqsQueueOutAttribute).GetProperty(nameof(SqsQueueOutAttribute.QueueUrl));
        var attributes = property!.GetCustomAttributes(
            typeof(Microsoft.Azure.WebJobs.Description.AutoResolveAttribute), 
            false);

        // Assert
        attributes.Should().HaveCount(1);
    }

    [Fact]
    public void AWSKeyId_HasAutoResolveAttribute()
    {
        // Arrange & Act
        var property = typeof(SqsQueueOutAttribute).GetProperty(nameof(SqsQueueOutAttribute.AWSKeyId));
        var attributes = property!.GetCustomAttributes(
            typeof(Microsoft.Azure.WebJobs.Description.AutoResolveAttribute), 
            false);

        // Assert
        attributes.Should().HaveCount(1);
    }

    [Fact]
    public void AWSAccessKey_HasAutoResolveAttribute()
    {
        // Arrange & Act
        var property = typeof(SqsQueueOutAttribute).GetProperty(nameof(SqsQueueOutAttribute.AWSAccessKey));
        var attributes = property!.GetCustomAttributes(
            typeof(Microsoft.Azure.WebJobs.Description.AutoResolveAttribute), 
            false);

        // Assert
        attributes.Should().HaveCount(1);
    }

    [Fact]
    public void Region_HasAutoResolveAttribute()
    {
        // Arrange & Act
        var property = typeof(SqsQueueOutAttribute).GetProperty(nameof(SqsQueueOutAttribute.Region));
        var attributes = property!.GetCustomAttributes(
            typeof(Microsoft.Azure.WebJobs.Description.AutoResolveAttribute), 
            false);

        // Assert
        attributes.Should().HaveCount(1);
    }

    #endregion
}
