using Azure.Functions.Worker.Extensions.EventBridge;
using Azure.Functions.Worker.Extensions.SNS;
using Azure.Functions.Worker.Extensions.S3;
using Azure.Functions.Worker.Extensions.Kinesis;

namespace Extensions.AWS.UnitTests;

/// <summary>
/// Unit tests for Azure Functions Worker extensions.
/// These tests verify the extensions can be loaded and basic types are accessible.
/// </summary>
public class ExtensionLoadingTests
{
    #region EventBridge Tests

    [Fact]
    public void EventBridgeTriggerAttribute_CanBeInstantiated()
    {
        var attribute = new EventBridgeTriggerAttribute("https://sqs.us-east-1.amazonaws.com/123456789/my-queue");
        Assert.NotNull(attribute);
        Assert.Equal("https://sqs.us-east-1.amazonaws.com/123456789/my-queue", attribute.QueueUrl);
    }

    [Fact]
    public void EventBridgeTriggerAttribute_SetsPropertiesCorrectly()
    {
        var attribute = new EventBridgeTriggerAttribute("https://sqs.us-east-1.amazonaws.com/123456789/my-queue")
        {
            Region = "us-east-1",
            AWSKeyId = "test-key",
            AWSAccessKey = "test-secret",
            MaxNumberOfMessages = 5
        };

        Assert.Equal("https://sqs.us-east-1.amazonaws.com/123456789/my-queue", attribute.QueueUrl);
        Assert.Equal("us-east-1", attribute.Region);
        Assert.Equal("test-key", attribute.AWSKeyId);
        Assert.Equal("test-secret", attribute.AWSAccessKey);
        Assert.Equal(5, attribute.MaxNumberOfMessages);
    }

    [Fact]
    public void EventBridgeOutputAttribute_CanBeInstantiated()
    {
        var attribute = new EventBridgeOutputAttribute("my-event-bus");
        Assert.NotNull(attribute);
        Assert.Equal("my-event-bus", attribute.EventBusName);
    }

    [Fact]
    public void EventBridgeOutputAttribute_SetsPropertiesCorrectly()
    {
        var attribute = new EventBridgeOutputAttribute("my-event-bus")
        {
            Region = "us-east-1",
            Source = "my-app",
            DetailType = "MyEvent"
        };

        Assert.Equal("my-event-bus", attribute.EventBusName);
        Assert.Equal("us-east-1", attribute.Region);
        Assert.Equal("my-app", attribute.Source);
        Assert.Equal("MyEvent", attribute.DetailType);
    }

    #endregion

    #region SNS Tests

    [Fact]
    public void SnsTriggerAttribute_CanBeInstantiated()
    {
        var attribute = new SnsTriggerAttribute("https://sqs.us-east-1.amazonaws.com/123456789/sns-queue");
        Assert.NotNull(attribute);
        Assert.Equal("https://sqs.us-east-1.amazonaws.com/123456789/sns-queue", attribute.QueueUrl);
    }

    [Fact]
    public void SnsTriggerAttribute_SetsPropertiesCorrectly()
    {
        var attribute = new SnsTriggerAttribute("https://sqs.us-east-1.amazonaws.com/123456789/sns-queue")
        {
            Region = "us-west-2",
            MaxNumberOfMessages = 5
        };

        Assert.Equal("https://sqs.us-east-1.amazonaws.com/123456789/sns-queue", attribute.QueueUrl);
        Assert.Equal("us-west-2", attribute.Region);
        Assert.Equal(5, attribute.MaxNumberOfMessages);
    }

    [Fact]
    public void SnsOutputAttribute_CanBeInstantiated()
    {
        var attribute = new SnsOutputAttribute("arn:aws:sns:us-east-1:123456789:my-topic");
        Assert.NotNull(attribute);
        Assert.Equal("arn:aws:sns:us-east-1:123456789:my-topic", attribute.TopicArn);
    }

    [Fact]
    public void SnsOutputAttribute_SetsPropertiesCorrectly()
    {
        var attribute = new SnsOutputAttribute("arn:aws:sns:us-east-1:123456789:my-topic")
        {
            Region = "us-east-1",
            Subject = "Test Subject"
        };

        Assert.Equal("arn:aws:sns:us-east-1:123456789:my-topic", attribute.TopicArn);
        Assert.Equal("us-east-1", attribute.Region);
        Assert.Equal("Test Subject", attribute.Subject);
    }

    #endregion

    #region S3 Tests

    [Fact]
    public void S3TriggerAttribute_CanBeInstantiated()
    {
        var attribute = new S3TriggerAttribute("https://sqs.us-east-1.amazonaws.com/123456789/s3-queue");
        Assert.NotNull(attribute);
        Assert.Equal("https://sqs.us-east-1.amazonaws.com/123456789/s3-queue", attribute.QueueUrl);
    }

    [Fact]
    public void S3TriggerAttribute_SetsPropertiesCorrectly()
    {
        var attribute = new S3TriggerAttribute("https://sqs.us-east-1.amazonaws.com/123456789/s3-queue")
        {
            Region = "eu-west-1",
            EventType = "s3:ObjectCreated:*",
            KeyPrefix = "uploads/",
            KeySuffix = ".json"
        };

        Assert.Equal("https://sqs.us-east-1.amazonaws.com/123456789/s3-queue", attribute.QueueUrl);
        Assert.Equal("eu-west-1", attribute.Region);
        Assert.Equal("s3:ObjectCreated:*", attribute.EventType);
        Assert.Equal("uploads/", attribute.KeyPrefix);
        Assert.Equal(".json", attribute.KeySuffix);
    }

    [Fact]
    public void S3OutputAttribute_CanBeInstantiated()
    {
        var attribute = new S3OutputAttribute("my-bucket");
        Assert.NotNull(attribute);
        Assert.Equal("my-bucket", attribute.BucketName);
    }

    [Fact]
    public void S3OutputAttribute_SetsPropertiesCorrectly()
    {
        var attribute = new S3OutputAttribute("my-bucket")
        {
            Region = "us-east-1",
            KeyPrefix = "output/",
            ContentType = "application/json"
        };

        Assert.Equal("my-bucket", attribute.BucketName);
        Assert.Equal("us-east-1", attribute.Region);
        Assert.Equal("output/", attribute.KeyPrefix);
        Assert.Equal("application/json", attribute.ContentType);
    }

    #endregion

    #region Kinesis Tests

    [Fact]
    public void KinesisTriggerAttribute_CanBeInstantiated()
    {
        var attribute = new KinesisTriggerAttribute("my-stream");
        Assert.NotNull(attribute);
        Assert.Equal("my-stream", attribute.StreamName);
    }

    [Fact]
    public void KinesisTriggerAttribute_SetsPropertiesCorrectly()
    {
        var attribute = new KinesisTriggerAttribute("my-stream")
        {
            Region = "ap-southeast-1",
            StartingPosition = "LATEST",
            BatchSize = 50
        };

        Assert.Equal("my-stream", attribute.StreamName);
        Assert.Equal("ap-southeast-1", attribute.Region);
        Assert.Equal("LATEST", attribute.StartingPosition);
        Assert.Equal(50, attribute.BatchSize);
    }

    [Fact]
    public void KinesisOutputAttribute_CanBeInstantiated()
    {
        var attribute = new KinesisOutputAttribute("output-stream");
        Assert.NotNull(attribute);
        Assert.Equal("output-stream", attribute.StreamName);
    }

    [Fact]
    public void KinesisOutputAttribute_SetsPropertiesCorrectly()
    {
        var attribute = new KinesisOutputAttribute("output-stream")
        {
            Region = "us-east-1",
            PartitionKey = "default-key"
        };

        Assert.Equal("output-stream", attribute.StreamName);
        Assert.Equal("us-east-1", attribute.Region);
        Assert.Equal("default-key", attribute.PartitionKey);
    }

    #endregion
}
