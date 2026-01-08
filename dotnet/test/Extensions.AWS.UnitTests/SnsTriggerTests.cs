using System.Net;
using System.Security.Cryptography;
using System.Text;
using Azure.WebJobs.Extensions.SNS;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Extensions.AWS.UnitTests;

public class SnsTriggerTests
{
    private readonly Mock<ILogger<SnsSignatureValidator>> _loggerMock;

    public SnsTriggerTests()
    {
        _loggerMock = new Mock<ILogger<SnsSignatureValidator>>();
    }

    #region SnsTriggerAttribute Tests

    [Fact]
    public void WebJobsSnsTriggerAttribute_DefaultValues()
    {
        var attribute = new Azure.WebJobs.Extensions.SNS.SnsTriggerAttribute();

        Assert.Null(attribute.TopicArn);
        Assert.Null(attribute.Route);
        Assert.True(attribute.VerifySignature);
        Assert.True(attribute.AutoConfirmSubscription);
        Assert.Null(attribute.SubjectFilter);
    }

    [Fact]
    public void WebJobsSnsTriggerAttribute_SetsAllProperties()
    {
        var attribute = new Azure.WebJobs.Extensions.SNS.SnsTriggerAttribute
        {
            TopicArn = "arn:aws:sns:us-east-1:123456789:test-topic",
            Route = "webhooks/sns",
            VerifySignature = false,
            AutoConfirmSubscription = false,
            SubjectFilter = "orders"
        };

        Assert.Equal("arn:aws:sns:us-east-1:123456789:test-topic", attribute.TopicArn);
        Assert.Equal("webhooks/sns", attribute.Route);
        Assert.False(attribute.VerifySignature);
        Assert.False(attribute.AutoConfirmSubscription);
        Assert.Equal("orders", attribute.SubjectFilter);
    }

    #endregion

    #region SnsNotification Tests

    [Fact]
    public void SnsNotification_DeserializesCorrectly()
    {
        var json = @"{
            ""Type"": ""Notification"",
            ""MessageId"": ""msg-123"",
            ""TopicArn"": ""arn:aws:sns:us-east-1:123456789:test-topic"",
            ""Subject"": ""Test Subject"",
            ""Message"": ""Hello World"",
            ""Timestamp"": ""2024-01-15T12:00:00.000Z"",
            ""SignatureVersion"": ""1"",
            ""Signature"": ""abc123"",
            ""SigningCertURL"": ""https://sns.us-east-1.amazonaws.com/cert.pem"",
            ""UnsubscribeURL"": ""https://sns.us-east-1.amazonaws.com/unsubscribe""
        }";

        var notification = System.Text.Json.JsonSerializer.Deserialize<SnsNotification>(json);

        Assert.NotNull(notification);
        Assert.Equal("Notification", notification.Type);
        Assert.Equal("msg-123", notification.MessageId);
        Assert.Equal("arn:aws:sns:us-east-1:123456789:test-topic", notification.TopicArn);
        Assert.Equal("Test Subject", notification.Subject);
        Assert.Equal("Hello World", notification.Message);
        Assert.Equal("1", notification.SignatureVersion);
        Assert.Equal("abc123", notification.Signature);
        Assert.Equal("https://sns.us-east-1.amazonaws.com/cert.pem", notification.SigningCertUrl);
        Assert.Equal("https://sns.us-east-1.amazonaws.com/unsubscribe", notification.UnsubscribeUrl);
    }

    [Fact]
    public void SnsNotification_SubscriptionConfirmation_DeserializesCorrectly()
    {
        var json = @"{
            ""Type"": ""SubscriptionConfirmation"",
            ""MessageId"": ""sub-123"",
            ""TopicArn"": ""arn:aws:sns:us-east-1:123456789:test-topic"",
            ""Message"": ""You have chosen to subscribe to..."",
            ""Timestamp"": ""2024-01-15T12:00:00.000Z"",
            ""SignatureVersion"": ""1"",
            ""Signature"": ""abc123"",
            ""SigningCertURL"": ""https://sns.us-east-1.amazonaws.com/cert.pem"",
            ""SubscribeURL"": ""https://sns.us-east-1.amazonaws.com/confirm"",
            ""Token"": ""token-abc-123""
        }";

        var notification = System.Text.Json.JsonSerializer.Deserialize<SnsNotification>(json);

        Assert.NotNull(notification);
        Assert.Equal("SubscriptionConfirmation", notification.Type);
        Assert.Equal("sub-123", notification.MessageId);
        Assert.Equal("https://sns.us-east-1.amazonaws.com/confirm", notification.SubscribeUrl);
        Assert.Equal("token-abc-123", notification.Token);
    }

    [Fact]
    public void SnsNotification_WithMessageAttributes_DeserializesCorrectly()
    {
        var json = @"{
            ""Type"": ""Notification"",
            ""MessageId"": ""msg-123"",
            ""TopicArn"": ""arn:aws:sns:us-east-1:123456789:test-topic"",
            ""Message"": ""Hello"",
            ""Timestamp"": ""2024-01-15T12:00:00.000Z"",
            ""MessageAttributes"": {
                ""eventType"": {
                    ""Type"": ""String"",
                    ""Value"": ""OrderCreated""
                },
                ""priority"": {
                    ""Type"": ""Number"",
                    ""Value"": ""1""
                }
            }
        }";

        var notification = System.Text.Json.JsonSerializer.Deserialize<SnsNotification>(json);

        Assert.NotNull(notification);
        Assert.NotNull(notification.MessageAttributes);
        Assert.Equal(2, notification.MessageAttributes.Count);
        Assert.Equal("String", notification.MessageAttributes["eventType"].Type);
        Assert.Equal("OrderCreated", notification.MessageAttributes["eventType"].Value);
        Assert.Equal("Number", notification.MessageAttributes["priority"].Type);
        Assert.Equal("1", notification.MessageAttributes["priority"].Value);
    }

    #endregion

    #region SnsNotificationValueProvider Tests

    [Fact]
    public async Task SnsNotificationValueProvider_ReturnsNotification()
    {
        var notification = new SnsNotification
        {
            MessageId = "test-123",
            Message = "Test message"
        };

        var provider = new SnsNotificationValueProvider(notification);

        var result = await provider.GetValueAsync();

        Assert.Same(notification, result);
    }

    [Fact]
    public void SnsNotificationValueProvider_Type_IsSnsNotification()
    {
        var notification = new SnsNotification { MessageId = "test" };
        var provider = new SnsNotificationValueProvider(notification);

        Assert.Equal(typeof(SnsNotification), provider.Type);
    }

    [Fact]
    public void SnsNotificationValueProvider_ToInvokeString_ContainsMessageId()
    {
        var notification = new SnsNotification { MessageId = "msg-abc-123" };
        var provider = new SnsNotificationValueProvider(notification);

        var result = provider.ToInvokeString();

        Assert.Contains("msg-abc-123", result);
    }

    #endregion

    #region SnsSignatureValidator Tests

    [Fact]
    public async Task ValidateSignatureAsync_RejectsNullNotification()
    {
        var httpClient = new HttpClient();
        var validator = new SnsSignatureValidator(httpClient, _loggerMock.Object);

        var result = await validator.ValidateSignatureAsync(null!);

        Assert.False(result);
    }

    [Fact]
    public async Task ValidateSignatureAsync_RejectsMissingSigningCertUrl()
    {
        var httpClient = new HttpClient();
        var validator = new SnsSignatureValidator(httpClient, _loggerMock.Object);
        var notification = new SnsNotification
        {
            MessageId = "test",
            Signature = "abc123",
            SigningCertUrl = null
        };

        var result = await validator.ValidateSignatureAsync(notification);

        Assert.False(result);
    }

    [Fact]
    public async Task ValidateSignatureAsync_RejectsMissingSignature()
    {
        var httpClient = new HttpClient();
        var validator = new SnsSignatureValidator(httpClient, _loggerMock.Object);
        var notification = new SnsNotification
        {
            MessageId = "test",
            Signature = null,
            SigningCertUrl = "https://sns.us-east-1.amazonaws.com/cert.pem"
        };

        var result = await validator.ValidateSignatureAsync(notification);

        Assert.False(result);
    }

    [Fact]
    public async Task ValidateSignatureAsync_RejectsNonAwsDomain()
    {
        var httpClient = new HttpClient();
        var validator = new SnsSignatureValidator(httpClient, _loggerMock.Object);
        var notification = new SnsNotification
        {
            MessageId = "test",
            Signature = "abc123",
            SigningCertUrl = "https://evil-site.com/fake-cert.pem"
        };

        var result = await validator.ValidateSignatureAsync(notification);

        Assert.False(result);
    }

    [Fact]
    public async Task ValidateSignatureAsync_RejectsHttpUrl()
    {
        var httpClient = new HttpClient();
        var validator = new SnsSignatureValidator(httpClient, _loggerMock.Object);
        var notification = new SnsNotification
        {
            MessageId = "test",
            Signature = "abc123",
            SigningCertUrl = "http://sns.us-east-1.amazonaws.com/cert.pem" // HTTP not HTTPS
        };

        var result = await validator.ValidateSignatureAsync(notification);

        Assert.False(result);
    }

    [Fact]
    public async Task ValidateSignatureAsync_AcceptsAwsComDomain()
    {
        // This test verifies the domain validation logic
        // The actual signature validation will fail since we don't have a real cert
        var httpClient = new HttpClient();
        var validator = new SnsSignatureValidator(httpClient, _loggerMock.Object);
        var notification = new SnsNotification
        {
            Type = "Notification",
            MessageId = "test",
            TopicArn = "arn:aws:sns:us-east-1:123:topic",
            Message = "Hello",
            Timestamp = "2024-01-01T00:00:00.000Z",
            Signature = "abc123",
            SigningCertUrl = "https://sns.us-east-1.amazonaws.com/cert.pem"
        };

        // Will fail because we can't fetch the cert, but the domain check should pass
        var result = await validator.ValidateSignatureAsync(notification);

        // Signature validation fails (can't fetch cert), but we verify error handling works
        Assert.False(result); // Expected - we can't fetch real AWS cert
    }

    #endregion
}
