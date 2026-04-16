# Authentication

The SQS extension supports three ways for an Azure Function to authenticate to AWS. They are listed below in order of recommendation.

| # | Pattern | Password-less for AWS? | Password-less in Azure? | Recommended for |
|---|---------|------------------------|--------------------------|------------------|
| 1 | **Entra ID federation via Managed Identity** | ✅ Yes | ✅ Yes | **Production. This is the recommended option.** |
| 2 | Entra ID federation via App Registration (client secret) | ✅ Yes | ⚠️ No — Entra client secret required | When managed identity isn't available (e.g. some local dev setups) |
| 3 | AWS default credential chain (env vars / shared credentials file) | ❌ No — long-lived AWS key | ❌ No — long-lived AWS key in env vars | Existing deployments where the AWS key already lives in app settings / Key Vault |
| 4 | AWS access key + secret on the binding attribute | ❌ No — long-lived AWS key | ❌ No — long-lived AWS key in Azure | Backwards compatibility only |

The extension picks credentials in this priority order:

1. **Federation (option 1 or 2)** when `AwsRoleArn` is set on the binding attribute.
2. **Explicit keys (option 4)** when both `AWSKeyId` and `AWSAccessKey` are set on the attribute.
3. **AWS default credential chain (option 3)** otherwise — the AWS SDK looks up `AWS_ACCESS_KEY_ID` / `AWS_SECRET_ACCESS_KEY` env vars, the shared credentials file, etc.

Federation always wins when configured.

---

## 1. Managed Identity → AWS (recommended, password-less)

No secret lives anywhere in your Azure configuration. The Function App's managed identity gets an Entra ID token, which AWS STS exchanges for short-lived AWS credentials (default 1 hour, refreshed automatically).

### How it works

```
┌────────────────────────────────────────────────────────────┐
│  Azure Function App (managed identity enabled)             │
└──────────────────────────┬─────────────────────────────────┘
                           │  (1) DefaultAzureCredential.GetTokenAsync
                           │      audience = api://AWSSecurityTokenService
                           ▼
┌────────────────────────────────────────────────────────────┐
│  Entra ID                                                  │
│  - issues a JWT signed with the tenant key                 │
│  - sub = managed identity object ID                        │
│  - aud = api://AWSSecurityTokenService                     │
└──────────────────────────┬─────────────────────────────────┘
                           │  (2) Entra JWT
                           ▼
┌────────────────────────────────────────────────────────────┐
│  AWS STS — AssumeRoleWithWebIdentity                       │
│  - validates JWT signature via the IAM OIDC provider       │
│  - checks trust policy: aud + sub + issuer must match      │
│  - returns temporary credentials (1h default, refreshed)   │
└──────────────────────────┬─────────────────────────────────┘
                           │  (3) AccessKey + Secret + SessionToken
                           ▼
┌────────────────────────────────────────────────────────────┐
│  AmazonSQSClient (inside the Function App)                 │
│  - signs SQS calls with the temporary credentials          │
│  - the EntraIdFederatedCredentials class auto-refreshes    │
│    the token before expiry, so step (1)–(3) repeats        │
└──────────────────────────┬─────────────────────────────────┘
                           │  (4) ReceiveMessage / SendMessage / DeleteMessage
                           ▼
┌────────────────────────────────────────────────────────────┐
│  AWS SQS                                                   │
└────────────────────────────────────────────────────────────┘
```

### One-time AWS setup

Create an OIDC identity provider in IAM:

- **Provider URL**: `https://sts.windows.net/<your-tenant-id>/`
- **Audience**: `api://AWSSecurityTokenService`

Create an IAM role with this trust policy (replace tenant ID and managed identity object ID):

```json
{
  "Version": "2012-10-17",
  "Statement": [{
    "Effect": "Allow",
    "Principal": {
      "Federated": "arn:aws:iam::123456789012:oidc-provider/sts.windows.net/<tenant-id>/"
    },
    "Action": "sts:AssumeRoleWithWebIdentity",
    "Condition": {
      "StringEquals": {
        "sts.windows.net/<tenant-id>/:aud": "api://AWSSecurityTokenService",
        "sts.windows.net/<tenant-id>/:sub": "<managed-identity-object-id>"
      }
    }
  }]
}
```

Attach an SQS permission policy to the role granting at least `sqs:ReceiveMessage`, `sqs:DeleteMessage` (trigger) and/or `sqs:SendMessage` (output) on your queue ARN.

### One-time Azure setup

Enable a system-assigned (or user-assigned) managed identity on the Function App.

### Function code

```csharp
public void Run(
    [SqsQueueTrigger(
        QueueUrl = "%SQS_QUEUE_URL%",
        AwsRoleArn = "%AWS_ROLE_ARN%")]
    Message message,
    ILogger logger)
{
    // ...
}
```

In app settings: `SQS_QUEUE_URL` and `AWS_ROLE_ARN` (the role created above). **No AWS credentials anywhere.**

---

## 2. App Registration → AWS

Same federation flow as option 1, but the Entra token is obtained using an Entra app registration's client secret instead of the Function App's managed identity. The AWS side still gets only short-lived STS credentials — but you do have to store the Entra client secret in Azure (Key Vault reference recommended).

When to choose this over option 1: usually only when managed identity isn't an option (some local dev scenarios; environments where you need a single identity that works across hosts).

### How it works

```
┌────────────────────────────────────────────────────────────┐
│  Azure Function App                                        │
│  settings: EntraTenantId, EntraClientId,                   │
│            EntraClientSecret  (Key Vault reference)        │
└──────────────────────────┬─────────────────────────────────┘
                           │  (1) ClientSecretCredential.GetTokenAsync
                           │      using tenant + client id + client secret
                           ▼
┌────────────────────────────────────────────────────────────┐
│  Entra ID (token endpoint)                                 │
│  - validates client_secret against the app registration    │
│  - issues a JWT signed with the tenant key                 │
│  - sub = app registration object ID                        │
│  - aud = api://AWSSecurityTokenService                     │
└──────────────────────────┬─────────────────────────────────┘
                           │  (2) Entra JWT
                           ▼
       (steps 3–5 are identical to option 1: AWS STS validates
        the JWT, returns temporary AWS credentials, the SQS
        client signs API calls with them)

                           ▼
┌────────────────────────────────────────────────────────────┐
│  AWS SQS                                                   │
└────────────────────────────────────────────────────────────┘
```

### AWS setup

Same as option 1, but the trust policy's `sub` condition uses the app registration's object ID (or the federated subject your IdP issues).

### Function code

```csharp
public void Run(
    [SqsQueueTrigger(
        QueueUrl = "%SQS_QUEUE_URL%",
        AwsRoleArn = "%AWS_ROLE_ARN%",
        EntraTenantId = "%ENTRA_TENANT_ID%",
        EntraClientId = "%ENTRA_CLIENT_ID%",
        EntraClientSecret = "@Microsoft.KeyVault(SecretUri=https://my-vault.vault.azure.net/secrets/entra-client-secret/)")]
    Message message,
    ILogger logger)
{
    // ...
}
```

> **Always reference `EntraClientSecret` from Key Vault** — never as a literal app setting. The example above uses an Azure Key Vault reference.

---

## 3. AWS default credential chain

Leave the AWS-specific attribute properties unset and let the AWS SDK's default credential provider chain locate credentials. On Azure (no EC2/ECS/EKS infrastructure), the practical pickup paths are:

- Environment variables: `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`, optional `AWS_SESSION_TOKEN`
- A shared credentials file (`~/.aws/credentials`) mounted into the Function App

Set those env vars as Function App settings (Key Vault references recommended for the secret).

### How it works

```
┌────────────────────────────────────────────────────────────┐
│  Azure Function App                                        │
│  env: AWS_ACCESS_KEY_ID, AWS_SECRET_ACCESS_KEY             │
│       (Key Vault reference recommended for the secret)     │
└──────────────────────────┬─────────────────────────────────┘
                           │  (1) AWS SDK default credential chain
                           │      walks env vars / shared file
                           ▼
┌────────────────────────────────────────────────────────────┐
│  AmazonSQSClient                                           │
│  - signs SQS calls with the long-lived credentials         │
└──────────────────────────┬─────────────────────────────────┘
                           │  (2) ReceiveMessage / SendMessage
                           ▼
┌────────────────────────────────────────────────────────────┐
│  AWS SQS                                                   │
└────────────────────────────────────────────────────────────┘
```

```csharp
public void Run(
    [SqsQueueTrigger(QueueUrl = "%SQS_QUEUE_URL%")]
    Message message,
    ILogger logger)
{
    // ...
}
```

The AWS secret still lives in Azure (just in env vars rather than the binding attribute). The advantage over option 4 is that you can rotate the key by editing app settings without redeploying code, and standard AWS tooling (CLI, SDKs, samples) "just works" in the same environment.

## 4. AWS access key + secret on the attribute (legacy)

Long-lived AWS credentials passed directly to the binding attribute. **Avoid for new code.** Kept for backwards compatibility with existing deployments.

### How it works

```
┌────────────────────────────────────────────────────────────┐
│  Azure Function App                                        │
│  binding attribute: AWSKeyId = "%AWS_ACCESS_KEY_ID%"       │
│                     AWSAccessKey = "%AWS_SECRET_ACCESS_KEY%"│
│  app settings:      AWS_ACCESS_KEY_ID, AWS_SECRET_ACCESS_KEY│
└──────────────────────────┬─────────────────────────────────┘
                           │  (1) %VAR% interpolation reads env vars
                           │      into the attribute at startup
                           ▼
┌────────────────────────────────────────────────────────────┐
│  AmazonSQSClient                                           │
│  - constructed with BasicAWSCredentials(key, secret)       │
└──────────────────────────┬─────────────────────────────────┘
                           │  (2) ReceiveMessage / SendMessage
                           ▼
┌────────────────────────────────────────────────────────────┐
│  AWS SQS                                                   │
└────────────────────────────────────────────────────────────┘
```

```csharp
public void Run(
    [SqsQueueTrigger(
        QueueUrl = "%SQS_QUEUE_URL%",
        AWSKeyId = "%AWS_ACCESS_KEY_ID%",
        AWSAccessKey = "%AWS_SECRET_ACCESS_KEY%")]
    Message message,
    ILogger logger)
{
    // ...
}
```

Same security posture as option 3 (a long-lived AWS secret lives in your Azure configuration). Rotate it on a regular cadence, and migrate to option 1 when you can.

---

## All federation knobs

| Property | Default | Description |
|----------|---------|-------------|
| `AwsRoleArn` | _(unset)_ | IAM role to assume. Setting this enables federation. |
| `AwsStsAudience` | `api://AWSSecurityTokenService` | Token audience. Must match the IAM trust policy. |
| `AwsRoleSessionName` | `azure-functions-sqs` | Surfaces in CloudTrail for auditing. |
| `AwsSessionDurationSeconds` | `3600` | STS session length. Must be ≤ the role's `MaxSessionDuration`. |
| `EntraTenantId` | _(unset)_ | Entra tenant for option 2. Leave unset for option 1. |
| `EntraClientId` | _(unset)_ | Entra app registration client ID for option 2. |
| `EntraClientSecret` | _(unset)_ | Entra app registration secret for option 2. **Use a Key Vault reference.** |

These properties exist on `SqsQueueTriggerAttribute`, `SqsQueueOutAttribute` (host-side / in-process) and `SqsQueueTriggerAttribute`, `SqsOutputAttribute` (worker-side / isolated).

---

## Local development

When `AwsRoleArn` is set but no app-reg fields are provided, the extension uses `DefaultAzureCredential`, which transparently picks up:

- The Function App's managed identity (in production)
- `az login` credentials (local dev with the Azure CLI)
- Visual Studio / VS Code signed-in identity

Add your developer Entra object ID as an additional principal in the IAM role's trust policy condition (`sts.windows.net/<tenant>/:sub` → the developer's object ID) and federation works locally with no code change.

If you can't federate the developer identity, set `AZURE_TENANT_ID` / `AZURE_CLIENT_ID` / `AZURE_CLIENT_SECRET` env vars locally — `DefaultAzureCredential` picks those up via its `EnvironmentCredential` step.

---

## Required IAM permissions

The IAM role assumed via federation needs:

- `sqs:ReceiveMessage`, `sqs:DeleteMessage`, `sqs:GetQueueAttributes` — for the trigger
- `sqs:SendMessage` — for the output binding
- `sqs:GetQueueUrl` — useful when the queue URL isn't hard-coded

Scope the resource to your specific queue ARN.
