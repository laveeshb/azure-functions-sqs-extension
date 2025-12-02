
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
}
