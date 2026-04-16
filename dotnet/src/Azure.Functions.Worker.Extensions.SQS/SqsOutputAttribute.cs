namespace Azure.Functions.Worker.Extensions.SQS;

using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

/// <summary>
/// Attribute used to configure an Amazon SQS output binding.
/// Compatible with Azure Functions isolated worker model.
/// Use on return type properties or method to send messages to SQS.
/// </summary>
public sealed class SqsOutputAttribute : OutputBindingAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqsOutputAttribute"/> class.
    /// </summary>
    /// <param name="queueUrl">The URL of the SQS queue to send messages to.</param>
    public SqsOutputAttribute(string queueUrl)
    {
        QueueUrl = queueUrl ?? throw new ArgumentNullException(nameof(queueUrl));
    }

    /// <summary>
    /// Gets the URL of the SQS queue to send messages to.
    /// </summary>
    public string QueueUrl { get; }

    /// <summary>
    /// Gets or sets the AWS Access Key ID. If not specified, uses AWS credential chain.
    /// </summary>
    public string? AWSKeyId { get; set; }

    /// <summary>
    /// Gets or sets the AWS Secret Access Key. If not specified, uses AWS credential chain.
    /// </summary>
    public string? AWSAccessKey { get; set; }

    /// <summary>
    /// Gets or sets the AWS Region (e.g., "us-east-1"). If not specified, uses AWS credential chain.
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// AWS IAM Role ARN to assume via Entra ID OIDC federation. When set, the extension
    /// authenticates by exchanging an Entra ID token (from the Function App's managed
    /// identity, or DefaultAzureCredential locally) for temporary AWS credentials via
    /// STS AssumeRoleWithWebIdentity — no AWS secret needs to be stored. Takes precedence
    /// over AWSKeyId/AWSAccessKey and the AWS default credential chain.
    /// </summary>
    public string? AwsRoleArn { get; set; }

    /// <summary>
    /// Entra ID token audience used when federating to AWS. Defaults to
    /// "api://AWSSecurityTokenService". Must match the AWS IAM trust policy.
    /// </summary>
    public string AwsStsAudience { get; set; } = "api://AWSSecurityTokenService";

    /// <summary>
    /// Session name passed to STS AssumeRoleWithWebIdentity. Surfaces in CloudTrail
    /// for auditing. Defaults to "azure-functions-sqs".
    /// </summary>
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
    public string? EntraTenantId { get; set; }

    /// <summary>
    /// Entra ID (Azure AD) client ID for an app registration. See <see cref="EntraTenantId"/>.
    /// </summary>
    public string? EntraClientId { get; set; }

    /// <summary>
    /// Entra ID (Azure AD) client secret for an app registration. Should be referenced from
    /// Azure Key Vault rather than stored as a literal app setting. Prefer managed identity
    /// (leave this unset) over an app registration with a secret.
    /// </summary>
    public string? EntraClientSecret { get; set; }
}
