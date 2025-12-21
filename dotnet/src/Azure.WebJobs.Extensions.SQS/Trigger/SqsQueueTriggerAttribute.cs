
namespace Azure.WebJobs.Extensions.SQS;

using System;
using Microsoft.Azure.WebJobs.Description;

[Binding]
[AttributeUsage(AttributeTargets.Parameter)]
public class SqsQueueTriggerAttribute : Attribute
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
    /// Custom SQS service URL for LocalStack or other SQS-compatible services.
    /// Example: "http://localhost:4566" for LocalStack.
    /// When specified, Region must also be provided.
    /// </summary>
    [AutoResolve]
    public string? ServiceUrl { get; set; }
}
