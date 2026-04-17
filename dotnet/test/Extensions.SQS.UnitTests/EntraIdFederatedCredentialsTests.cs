namespace Extensions.SQS.UnitTests;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.SecurityToken.Model;
using Azure.Core;
using Azure.WebJobs.Extensions.SQS.Auth;
using FluentAssertions;
using Microsoft.Extensions.Logging;
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

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(899)]
    public void Constructor_SessionDurationBelowMinimum_Throws(int durationSeconds)
    {
        // STS rejects assume-role sessions shorter than 900s. Surface the error at
        // startup rather than at first SQS call.
        var act = () => new EntraIdFederatedCredentials(
            roleArn: "arn:aws:iam::123456789012:role/r",
            audience: "api://AWSSecurityTokenService",
            roleSessionName: "s",
            sessionDurationSeconds: durationSeconds,
            stsRegion: RegionEndpoint.USEast1);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void GetCredentials_LogsDebugOnRefresh()
    {
        var logger = new RecordingLogger();
        var entraToken = new AccessToken("t", DateTimeOffset.UtcNow.AddMinutes(60));

        var creds = new EntraIdFederatedCredentials(
            roleArn: "arn:aws:iam::123456789012:role/r",
            audience: "api://AWSSecurityTokenService",
            roleSessionName: "s",
            sessionDurationSeconds: 3600,
            stsRegion: RegionEndpoint.USEast1,
            logger: logger,
            getEntraToken: (_, _) => new ValueTask<AccessToken>(entraToken),
            assumeRoleAsync: (_, _) => Task.FromResult(MakeStsResponse()));

        creds.GetCredentials();

        logger.Entries.Should().ContainSingle(e => e.Level == LogLevel.Debug);
    }

    [Fact]
    public void GetCredentials_LogsWarningWhenEntraTokenFails()
    {
        var logger = new RecordingLogger();

        var creds = new EntraIdFederatedCredentials(
            roleArn: "arn:aws:iam::123456789012:role/r",
            audience: "api://AWSSecurityTokenService",
            roleSessionName: "s",
            sessionDurationSeconds: 3600,
            stsRegion: RegionEndpoint.USEast1,
            logger: logger,
            getEntraToken: (_, _) => throw new InvalidOperationException("entra boom"),
            assumeRoleAsync: (_, _) => Task.FromResult(MakeStsResponse()));

        var act = () => creds.GetCredentials();

        act.Should().Throw<InvalidOperationException>().WithMessage("*entra boom*");
        logger.Entries.Should().Contain(e => e.Level == LogLevel.Warning && e.Exception is InvalidOperationException);
    }

    [Fact]
    public void GetCredentials_LogsWarningWhenStsFails()
    {
        var logger = new RecordingLogger();
        var entraToken = new AccessToken("t", DateTimeOffset.UtcNow.AddMinutes(60));

        var creds = new EntraIdFederatedCredentials(
            roleArn: "arn:aws:iam::123456789012:role/r",
            audience: "api://AWSSecurityTokenService",
            roleSessionName: "s",
            sessionDurationSeconds: 3600,
            stsRegion: RegionEndpoint.USEast1,
            logger: logger,
            getEntraToken: (_, _) => new ValueTask<AccessToken>(entraToken),
            assumeRoleAsync: (_, _) => throw new InvalidOperationException("sts boom"));

        var act = () => creds.GetCredentials();

        act.Should().Throw<InvalidOperationException>().WithMessage("*sts boom*");
        logger.Entries.Should().Contain(e => e.Level == LogLevel.Warning && e.Exception is InvalidOperationException);
    }

    private sealed class RecordingLogger : ILogger
    {
        public List<(LogLevel Level, Exception? Exception, string Message)> Entries { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, exception, formatter(state, exception)));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}
