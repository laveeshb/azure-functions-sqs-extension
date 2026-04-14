namespace Azure.Functions.Extensions.SQS.Test.InProcess;

using Azure.WebJobs.Extensions.S3;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

/// <summary>
/// Sample functions demonstrating S3 input and output bindings.
/// </summary>
public class S3Functions
{
    #region Input Binding Functions

    /// <summary>
    /// Reads a file from S3 as a string using input binding.
    /// Example: curl "http://localhost:7071/api/s3/read-string?key=data/config.json"
    /// </summary>
    [FunctionName(nameof(ReadS3AsString))]
    public IActionResult ReadS3AsString(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "s3/read-string")] HttpRequest req,
        [S3(BucketName = "%S3_BUCKET_NAME%", Key = "{Query.key}")] string content,
        ILogger log)
    {
        if (string.IsNullOrEmpty(content))
        {
            return new NotFoundObjectResult(new { error = "File not found" });
        }

        log.LogInformation("Read S3 object as string, length: {Length}", content.Length);

        return new OkObjectResult(new
        {
            contentType = "string",
            length = content.Length,
            content
        });
    }

    /// <summary>
    /// Reads a file from S3 as bytes using input binding.
    /// Example: curl "http://localhost:7071/api/s3/read-bytes?key=data/image.png"
    /// </summary>
    [FunctionName(nameof(ReadS3AsBytes))]
    public IActionResult ReadS3AsBytes(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "s3/read-bytes")] HttpRequest req,
        [S3(BucketName = "%S3_BUCKET_NAME%", Key = "{Query.key}")] byte[] content,
        ILogger log)
    {
        if (content == null || content.Length == 0)
        {
            return new NotFoundObjectResult(new { error = "File not found" });
        }

        log.LogInformation("Read S3 object as bytes, size: {Size}", content.Length);

        return new OkObjectResult(new
        {
            contentType = "bytes",
            size = content.Length,
            base64Preview = Convert.ToBase64String(content.Take(100).ToArray())
        });
    }

    /// <summary>
    /// Reads a file from S3 with full metadata using S3Object.
    /// Example: curl "http://localhost:7071/api/s3/read-object?key=documents/report.pdf"
    /// </summary>
    [FunctionName(nameof(ReadS3Object))]
    public IActionResult ReadS3Object(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "s3/read-object")] HttpRequest req,
        [S3(BucketName = "%S3_BUCKET_NAME%", Key = "{Query.key}")] S3Object s3Object,
        ILogger log)
    {
        if (s3Object == null)
        {
            return new NotFoundObjectResult(new { error = "File not found" });
        }

        log.LogInformation("Read S3 object: {Key}, ContentType: {ContentType}", 
            s3Object.Key, s3Object.ContentType);

        return new OkObjectResult(new
        {
            key = s3Object.Key,
            bucket = s3Object.BucketName,
            contentType = s3Object.ContentType,
            contentLength = s3Object.ContentLength,
            eTag = s3Object.ETag,
            lastModified = s3Object.LastModified,
            metadata = s3Object.Metadata
        });
    }

    /// <summary>
    /// Dynamic key binding with route parameter.
    /// Example: curl "http://localhost:7071/api/s3/documents/my-doc-id"
    /// </summary>
    [FunctionName(nameof(GetDocument))]
    public IActionResult GetDocument(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "s3/documents/{documentId}")] HttpRequest req,
        [S3(BucketName = "%S3_BUCKET_NAME%", Key = "documents/{documentId}.json")] string document,
        string documentId,
        ILogger log)
    {
        if (string.IsNullOrEmpty(document))
        {
            return new NotFoundObjectResult(new { error = $"Document {documentId} not found" });
        }

        log.LogInformation("Retrieved document: {DocumentId}", documentId);

        return new OkObjectResult(new
        {
            documentId,
            content = document
        });
    }

    #endregion

    #region Output Binding Functions

    /// <summary>
    /// Uploads content to S3 using output binding.
    /// Example: curl -X POST "http://localhost:7071/api/s3/upload?key=test.txt&content=Hello"
    /// </summary>
    [FunctionName(nameof(UploadToS3))]
    public async Task<IActionResult> UploadToS3(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "s3/upload")] HttpRequest req,
        [S3Out(BucketName = "%S3_BUCKET_NAME%")] IAsyncCollector<S3Message> objects,
        ILogger log)
    {
        var key = req.Query["key"].ToString();
        if (string.IsNullOrEmpty(key))
        {
            key = $"uploads/{Guid.NewGuid()}.txt";
        }

        var content = req.Query["content"].ToString();
        if (string.IsNullOrEmpty(content))
        {
            content = "Hello from Azure Functions!";
        }

        await objects.AddAsync(new S3Message
        {
            Key = key,
            Content = content,
            ContentType = "text/plain"
        });

        log.LogInformation("Uploaded to S3: {Key}", key);

        return new OkObjectResult(new
        {
            status = "Object uploaded to S3",
            key,
            contentLength = content.Length
        });
    }

    /// <summary>
    /// Uploads JSON content with metadata to S3.
    /// </summary>
    [FunctionName(nameof(UploadJsonToS3))]
    public async Task<IActionResult> UploadJsonToS3(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "s3/upload-json")] HttpRequest req,
        [S3Out(BucketName = "%S3_BUCKET_NAME%")] IAsyncCollector<S3Message> objects,
        ILogger log)
    {
        var documentId = Guid.NewGuid().ToString();
        var document = new
        {
            id = documentId,
            createdAt = DateTime.UtcNow,
            source = "Azure Functions"
        };

        await objects.AddAsync(new S3Message
        {
            Key = $"documents/{documentId}.json",
            Content = System.Text.Json.JsonSerializer.Serialize(document),
            ContentType = "application/json",
            Metadata = new Dictionary<string, string>
            {
                ["created-by"] = "azure-functions",
                ["document-type"] = "sample"
            }
        });

        log.LogInformation("Uploaded JSON document to S3: {DocumentId}", documentId);

        return new OkObjectResult(new
        {
            status = "JSON document uploaded to S3",
            documentId,
            key = $"documents/{documentId}.json"
        });
    }

    #endregion
}
