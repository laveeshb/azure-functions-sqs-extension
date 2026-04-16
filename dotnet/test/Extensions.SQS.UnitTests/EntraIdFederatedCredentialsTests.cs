namespace Extensions.SQS.UnitTests;

using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.SecurityToken.Model;
using Azure.Core;
using Azure.WebJobs.Extensions.SQS.Auth;
using FluentAssertions;
using Xunit;

public class EntraIdFederatedCredentialsTests
{
    private static AssumeRoleWithWebIdentityResponse MakeStsResponse(
        string accessKey = "ASIA-fake",
        string secret = "secret-fake",
        string session = "session-fake",
        int expiresInMinutes = 50)
    {
        return new AssumeRoleWithWebIdentityResponse
        {
            Credentials = new Credentials
            {
                AccessKeyId = accessKey,
                SecretAccessKey = secret,
                SessionToken = session,
                Expiration = DateTime.UtcNow.AddMinutes(expiresInMinutes),
            },
        };
    }

    [Fact]
    public void GetCredentials_ReturnsCredentialsFromStsResponse()
    {
        var entraToken = new AccessToken("fake-entra-token", DateTimeOffset.UtcNow.AddMinutes(60));
        var stsResponse = MakeStsResponse();

        var creds = new EntraIdFederatedCredentials(
            roleArn: "arn:aws:iam::123456789012:role/test-role",
            audience: "api://AWSSecurityTokenService",
            roleSessionName: "azure-functions-sqs",
            sessionDurationSeconds: 3600,
            stsRegion: RegionEndpoint.USEast1,
            getEntraToken: (_, _) => new ValueTask<AccessToken>(entraToken),
            assumeRoleAsync: (_, _) => Task.FromResult(stsResponse));

        var immutable = creds.GetCredentials();

        immutable.AccessKey.Should().Be("ASIA-fake");
        immutable.SecretKey.Should().Be("secret-fake");
        immutable.Token.Should().Be("session-fake");
    }

    [Fact]
    public void GetCredentials_PassesEntraTokenAndConfigToStsRequest()
    {
        var entraToken = new AccessToken("fake-entra-token", DateTimeOffset.UtcNow.AddMinutes(60));
        AssumeRoleWithWebIdentityRequest? captured = null;

        var creds = new EntraIdFederatedCredentials(
            roleArn: "arn:aws:iam::123456789012:role/my-role",
            audience: "api://AWSSecurityTokenService",
            roleSessionName: "my-session",
            sessionDurationSeconds: 1800,
            stsRegion: RegionEndpoint.USEast1,
            getEntraToken: (_, _) => new ValueTask<AccessToken>(entraToken),
            assumeRoleAsync: (req, _) =>
            {
                captured = req;
                return Task.FromResult(MakeStsResponse());
            });

        creds.GetCredentials();

        captured.Should().NotBeNull();
        captured!.RoleArn.Should().Be("arn:aws:iam::123456789012:role/my-role");
        captured.RoleSessionName.Should().Be("my-session");
        captured.WebIdentityToken.Should().Be("fake-entra-token");
        captured.DurationSeconds.Should().Be(1800);
    }

    [Fact]
    public void GetCredentials_PassesAudienceWithDotDefaultScopeToTokenProvider()
    {
        TokenRequestContext? capturedContext = null;
        var entraToken = new AccessToken("t", DateTimeOffset.UtcNow.AddMinutes(60));

        var creds = new EntraIdFederatedCredentials(
            roleArn: "arn:aws:iam::123456789012:role/r",
            audience: "api://CustomAudience",
            roleSessionName: "s",
            sessionDurationSeconds: 900,
            stsRegion: RegionEndpoint.USEast1,
            getEntraToken: (ctx, _) =>
            {
                capturedContext = ctx;
                return new ValueTask<AccessToken>(entraToken);
            },
            assumeRoleAsync: (_, _) => Task.FromResult(MakeStsResponse()));

        creds.GetCredentials();

        capturedContext.Should().NotBeNull();
        capturedContext!.Value.Scopes.Should().ContainSingle()
            .Which.Should().Be("api://CustomAudience/.default");
    }

    [Fact]
    public void GetCredentials_CachesCredentialsAndDoesNotRefetchWithinExpiry()
    {
        var entraToken = new AccessToken("t", DateTimeOffset.UtcNow.AddMinutes(60));
        var tokenCalls = 0;
        var stsCalls = 0;

        var creds = new EntraIdFederatedCredentials(
            roleArn: "arn:aws:iam::123456789012:role/r",
            audience: "api://AWSSecurityTokenService",
            roleSessionName: "s",
            sessionDurationSeconds: 3600,
            stsRegion: RegionEndpoint.USEast1,
            getEntraToken: (_, _) =>
            {
                Interlocked.Increment(ref tokenCalls);
                return new ValueTask<AccessToken>(entraToken);
            },
            assumeRoleAsync: (_, _) =>
            {
                Interlocked.Increment(ref stsCalls);
                return Task.FromResult(MakeStsResponse(expiresInMinutes: 50));
            });

        creds.GetCredentials();
        creds.GetCredentials();
        creds.GetCredentials();

        tokenCalls.Should().Be(1);
        stsCalls.Should().Be(1);
    }

    [Fact]
    public void Constructor_NullRoleArn_Throws()
    {
        var act = () => new EntraIdFederatedCredentials(
            roleArn: null!,
            audience: "api://AWSSecurityTokenService",
            roleSessionName: "s",
            sessionDurationSeconds: 3600,
            stsRegion: RegionEndpoint.USEast1);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullAudience_Throws()
    {
        var act = () => new EntraIdFederatedCredentials(
            roleArn: "arn:aws:iam::123456789012:role/r",
            audience: null!,
            roleSessionName: "s",
            sessionDurationSeconds: 3600,
            stsRegion: RegionEndpoint.USEast1);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullStsRegion_Throws()
    {
        var act = () => new EntraIdFederatedCredentials(
            roleArn: "arn:aws:iam::123456789012:role/r",
            audience: "api://AWSSecurityTokenService",
            roleSessionName: "s",
            sessionDurationSeconds: 3600,
            stsRegion: null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
