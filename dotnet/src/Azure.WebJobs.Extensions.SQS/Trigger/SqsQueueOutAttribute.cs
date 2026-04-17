
namespace Azure.WebJobs.Extensions.SQS;

using System;
using Microsoft.Azure.WebJobs.Description;

[AttributeUsage(AttributeTargets.Parameter)]
[Binding]
public class SqsQueueOutAttribute : Attribute
{
    /// <summary>
    /// AWS Access Key ID. Optional - if not provided, will use AWS credential chain (environment variables, IAM roles, etc.)
    /// </summary>
    [AutoResolve]
    public string? AWSKeyId { get; set; }

    /// <summary>
    /// AWS Secret Access Key. Optional - if not provided, will use AWS credential chain (environment variables, IAM roles, etc.)
    /// </summary>
    [AutoResolve]
    public string? AWSAccessKey { get; set; }

    /// <summary>
    /// SQS Queue URL (required)
    /// </summary>
    [AutoResolve]
    public string QueueUrl { get; set; } = string.Empty;

    /// <summary>
    /// AWS Region override. Optional - if not provided, will extract from QueueUrl
    /// </summary>
    [AutoResolve]
    public string? Region { get; set; }

    /// <summary>
    /// AWS IAM Role ARN to assume via Entra ID OIDC federation. When set, the extension
    /// authenticates by exchanging an Entra ID token (from the Function App's managed
    /// identity, or DefaultAzureCredential locally) for temporary AWS credentials via
    /// STS AssumeRoleWithWebIdentity — no AWS secret needs to be stored. Takes precedence
    /// over AWSKeyId/AWSAccessKey and the AWS default credential chain.
    /// </summary>
    [AutoResolve]
    public string? AwsRoleArn { get; set; }

    /// <summary>
    /// Entra ID token audience used when federating to AWS. Defaults to
    /// "api://AWSSecurityTokenService". Must match the AWS IAM trust policy.
    /// </summary>
    [AutoResolve]
    public string AwsStsAudience { get; set; } = "api://AWSSecurityTokenService";

    /// <summary>
    /// Session name passed to STS AssumeRoleWithWebIdentity. Surfaces in CloudTrail
    /// for auditing. Defaults to "azure-functions-sqs".
    /// </summary>
    [AutoResolve]
    public string AwsRoleSessionName { get; set; } = "azure-functions-sqs";

    /// <summary>
    /// Duration in seconds for the assumed-role session. Must be between 900 (15 min)
    /// and the maximum configured on the IAM role (default 3600). Defaults to 3600.
    /// </summary>
    public int AwsSessionDurationSeconds { get; set; } = 3600;

    /// <summary>
    /// Entra ID (Azure AD) tenant ID for an app registration used to obtain the federated
    /// token. Optional. When set together with EntraClientId and EntraClientSecret, the
    /// extension uses ClientSecretCredential. If left unset, DefaultAzureCredential is used,
    /// which picks up the Function App's managed identity in production — the recommended,
    /// password-less option.
    /// </summary>
    [AutoResolve]
    public string? EntraTenantId { get; set; }

    /// <summary>
    /// Entra ID (Azure AD) client ID for an app registration. See <see cref="EntraTenantId"/>.
    /// </summary>
    [AutoResolve]
    public string? EntraClientId { get; set; }

    /// <summary>
    /// Entra ID (Azure AD) client secret for an app registration. Should be referenced from
    /// Azure Key Vault rather than stored as a literal app setting. Prefer managed identity
    /// (leave this unset) over an app registration with a secret.
    /// </summary>
    [AutoResolve]
    public string? EntraClientSecret { get; set; }
}
