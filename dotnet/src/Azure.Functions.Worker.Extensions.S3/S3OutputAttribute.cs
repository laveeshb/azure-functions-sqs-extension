namespace Azure.Functions.Worker.Extensions.S3;

using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

/// <summary>
/// Attribute used to configure an Amazon S3 output binding.
/// Compatible with Azure Functions isolated worker model.
/// Use on return type properties or method to write objects to S3.
/// </summary>
public sealed class S3OutputAttribute : OutputBindingAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="S3OutputAttribute"/> class.
    /// </summary>
    /// <param name="bucketName">The name of the S3 bucket.</param>
    public S3OutputAttribute(string bucketName)
    {
        BucketName = bucketName ?? throw new ArgumentNullException(nameof(bucketName));
    }

    /// <summary>
    /// Gets the name of the S3 bucket.
    /// </summary>
    public string BucketName { get; }

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
    /// Gets or sets the default key prefix for uploaded objects.
    /// </summary>
    public string? KeyPrefix { get; set; }

    /// <summary>
    /// Gets or sets the default content type for uploaded objects.
    /// </summary>
    public string? ContentType { get; set; }
}
