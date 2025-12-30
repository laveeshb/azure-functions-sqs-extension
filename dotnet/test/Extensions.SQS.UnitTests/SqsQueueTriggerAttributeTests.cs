namespace Extensions.SQS.UnitTests;

using Azure.WebJobs.Extensions.SQS;
using FluentAssertions;
using Xunit;

public class SqsQueueTriggerAttributeTests
{
    #region Default Values Tests

    [Fact]
    public void QueueUrl_DefaultValue_IsEmptyString()
    {
        // Arrange & Act
        var attribute = new SqsQueueTriggerAttribute();

        // Assert
        attribute.QueueUrl.Should().Be(string.Empty);
    }

    [Fact]
    public void AWSKeyId_DefaultValue_IsNull()
    {
        // Arrange & Act
        var attribute = new SqsQueueTriggerAttribute();

        // Assert
        attribute.AWSKeyId.Should().BeNull();
    }

    [Fact]
    public void AWSAccessKey_DefaultValue_IsNull()
    {
        // Arrange & Act
        var attribute = new SqsQueueTriggerAttribute();

        // Assert
        attribute.AWSAccessKey.Should().BeNull();
    }

    [Fact]
    public void Region_DefaultValue_IsNull()
    {
        // Arrange & Act
        var attribute = new SqsQueueTriggerAttribute();

        // Assert
        attribute.Region.Should().BeNull();
    }

    #endregion

    #region Property Assignment Tests

    [Fact]
    public void QueueUrl_CanBeSet()
    {
        // Arrange
        var attribute = new SqsQueueTriggerAttribute();
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
        var attribute = new SqsQueueTriggerAttribute();
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
        var attribute = new SqsQueueTriggerAttribute();
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
        var attribute = new SqsQueueTriggerAttribute();
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
        var attributes = typeof(SqsQueueTriggerAttribute).GetCustomAttributes(
            typeof(Microsoft.Azure.WebJobs.Description.BindingAttribute), 
            false);

        // Assert
        attributes.Should().HaveCount(1);
    }

    [Fact]
    public void HasAttributeUsageForParameters()
    {
        // Arrange & Act
        var attributes = typeof(SqsQueueTriggerAttribute).GetCustomAttributes(
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
        var property = typeof(SqsQueueTriggerAttribute).GetProperty(nameof(SqsQueueTriggerAttribute.QueueUrl));
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
        var property = typeof(SqsQueueTriggerAttribute).GetProperty(nameof(SqsQueueTriggerAttribute.AWSKeyId));
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
        var property = typeof(SqsQueueTriggerAttribute).GetProperty(nameof(SqsQueueTriggerAttribute.AWSAccessKey));
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
        var property = typeof(SqsQueueTriggerAttribute).GetProperty(nameof(SqsQueueTriggerAttribute.Region));
        var attributes = property!.GetCustomAttributes(
            typeof(Microsoft.Azure.WebJobs.Description.AutoResolveAttribute), 
            false);

        // Assert
        attributes.Should().HaveCount(1);
    }

    #endregion

    #region Known Bugs - Expected Failures

    /// <summary>
    /// BUG: QueueUrl is not validated - empty string is accepted silently.
    /// This causes confusing AWS SDK errors at runtime instead of clear validation errors.
    /// 
    /// GitHub Issue: https://github.com/laveeshb/azure-functions-sqs-extension/issues/42
    /// Fix: Add validation in attribute or during binding that QueueUrl is a valid SQS URL.
    /// </summary>
    [Fact]
    public void QueueUrl_WhenEmpty_ShouldThrowValidationException()
    {
        // Arrange
        var attribute = new SqsQueueTriggerAttribute { QueueUrl = "" };

        // Act & Assert
        // Currently this does NOT throw - the empty QueueUrl is silently accepted
        // After fix, should throw InvalidOperationException or similar with clear message
        var action = () => attribute.QueueUrl.Should().NotBeNullOrEmpty(
            "QueueUrl is required and should be validated");
        
        // This assertion represents what the validation SHOULD do
        action.Should().NotThrow("attribute should validate QueueUrl is not empty");
    }

    /// <summary>
    /// BUG: QueueUrl accepts any string without validating it's a proper SQS URL format.
    /// 
    /// GitHub Issue: https://github.com/laveeshb/azure-functions-sqs-extension/issues/42
    /// Fix: Validate URL format matches SQS pattern: https://sqs.{region}.amazonaws.com/{account}/{queue}
    /// </summary>
    [Theory]
    [InlineData("not-a-url")]
    [InlineData("https://example.com/queue")]
    [InlineData("ftp://sqs.us-east-1.amazonaws.com/123/queue")]
    public void QueueUrl_WhenInvalidFormat_ShouldThrowValidationException(string invalidUrl)
    {
        // Arrange
        var attribute = new SqsQueueTriggerAttribute { QueueUrl = invalidUrl };

        // Act & Assert
        // Currently accepts any string - error only occurs later in AmazonSQSClientFactory
        // After fix, attribute itself should validate the URL format
        attribute.QueueUrl.Should().MatchRegex(
            @"^https://sqs\.[a-z0-9-]+\.amazonaws\.com/\d+/.+$",
            "QueueUrl should be a valid SQS URL");
    }

    #endregion
}
