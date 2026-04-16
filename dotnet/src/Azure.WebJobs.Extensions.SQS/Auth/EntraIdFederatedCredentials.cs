namespace Azure.WebJobs.Extensions.SQS.Auth;

using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using AzureCore = global::Azure.Core;
using AzureIdentity = global::Azure.Identity;

/// <summary>
/// AWS credentials provider that exchanges an Entra ID (Azure AD) access token for
/// temporary AWS credentials via STS AssumeRoleWithWebIdentity. Refreshes automatically
/// before expiry. Used when <c>AwsRoleArn</c> is set on the SQS trigger or output binding.
/// </summary>
public sealed class EntraIdFederatedCredentials : RefreshingAWSCredentials
{
    private readonly string _roleArn;
    private readonly string _audience;
    private readonly string _roleSessionName;
    private readonly int _sessionDurationSeconds;
    private readonly RegionEndpoint _stsRegion;
    private readonly Func<AzureCore.TokenRequestContext, CancellationToken, ValueTask<AzureCore.AccessToken>> _getEntraToken;
    private readonly Func<AssumeRoleWithWebIdentityRequest, CancellationToken, Task<AssumeRoleWithWebIdentityResponse>> _assumeRoleAsync;

    public EntraIdFederatedCredentials(
        string roleArn,
        string audience,
        string roleSessionName,
        int sessionDurationSeconds,
        RegionEndpoint stsRegion,
        Func<AzureCore.TokenRequestContext, CancellationToken, ValueTask<AzureCore.AccessToken>>? getEntraToken = null,
        Func<AssumeRoleWithWebIdentityRequest, CancellationToken, Task<AssumeRoleWithWebIdentityResponse>>? assumeRoleAsync = null)
    {
        _roleArn = roleArn ?? throw new ArgumentNullException(nameof(roleArn));
        _audience = audience ?? throw new ArgumentNullException(nameof(audience));
        _roleSessionName = roleSessionName ?? throw new ArgumentNullException(nameof(roleSessionName));
        _sessionDurationSeconds = sessionDurationSeconds;
        _stsRegion = stsRegion ?? throw new ArgumentNullException(nameof(stsRegion));
        _getEntraToken = getEntraToken ?? new AzureIdentity.DefaultAzureCredential().GetTokenAsync;
        _assumeRoleAsync = assumeRoleAsync ?? DefaultAssumeRoleAsync;
    }

    protected override CredentialsRefreshState GenerateNewCredentials()
    {
        // The AWS SDK uses GetCredentialsAsync on the async request path (which is
        // what AmazonSQSClient.ReceiveMessageAsync hits). This sync override exists
        // for any sync callers; offload to the thread pool to avoid sync-context deadlocks.
        return Task.Run(GenerateNewCredentialsAsync).GetAwaiter().GetResult();
    }

    protected override async Task<CredentialsRefreshState> GenerateNewCredentialsAsync()
    {
        var entraToken = await _getEntraToken(
            new AzureCore.TokenRequestContext(new[] { $"{_audience}/.default" }),
            CancellationToken.None).ConfigureAwait(false);

        var stsResponse = await _assumeRoleAsync(
            new AssumeRoleWithWebIdentityRequest
            {
                RoleArn = _roleArn,
                RoleSessionName = _roleSessionName,
                WebIdentityToken = entraToken.Token,
                DurationSeconds = _sessionDurationSeconds,
            },
            CancellationToken.None).ConfigureAwait(false);

        return new CredentialsRefreshState(
            new ImmutableCredentials(
                stsResponse.Credentials.AccessKeyId,
                stsResponse.Credentials.SecretAccessKey,
                stsResponse.Credentials.SessionToken),
            stsResponse.Credentials.Expiration);
    }

    private async Task<AssumeRoleWithWebIdentityResponse> DefaultAssumeRoleAsync(
        AssumeRoleWithWebIdentityRequest request,
        CancellationToken cancellationToken)
    {
        // STS AssumeRoleWithWebIdentity is unauthenticated — the web identity token
        // is the authentication. Anonymous credentials prevent the SDK from looking
        // for AWS credentials it doesn't need.
        using var stsClient = new AmazonSecurityTokenServiceClient(new AnonymousAWSCredentials(), _stsRegion);
        return await stsClient.AssumeRoleWithWebIdentityAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
