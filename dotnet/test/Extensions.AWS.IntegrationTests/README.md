# AWS Extensions Integration Tests

This project contains integration tests that run against [LocalStack](https://localstack.cloud/) to verify the AWS SDK integrations work correctly.

## Prerequisites

- **.NET 8.0 SDK**
- **Docker** - Required for running LocalStack via Testcontainers

## Running Tests

### Using Scripts

**Linux/macOS:**
```bash
cd dotnet
./scripts/unix/integration-test.sh
```

**Windows:**
```powershell
cd dotnet
.\scripts\windows\integration-test.ps1
```

### Using dotnet CLI

```bash
cd dotnet
dotnet test test/Extensions.AWS.IntegrationTests --configuration Release
```

## How It Works

These tests use [Testcontainers](https://dotnet.testcontainers.org/) to automatically:
1. Pull the LocalStack Docker image
2. Start a LocalStack container
3. Run all integration tests against it
4. Clean up the container when done

No manual setup is required - just have Docker running!

## Test Coverage

| Service | Tests | Description |
|---------|-------|-------------|
| **SQS** | 9 | Queue CRUD, send/receive messages, batch operations, FIFO queues |
| **EventBridge** | 8 | Event bus CRUD, put events, rules management |
| **SNS** | 10 | Topic CRUD, publish messages, batch publish, FIFO topics |
| **S3** | 11 | Bucket CRUD, object upload/download, metadata, copy, batch delete |
| **Kinesis** | 10 | Stream CRUD, put/get records, shard iterators, batch operations |

## Test Structure

```
Extensions.AWS.IntegrationTests/
├── LocalStackFixture.cs        # Shared container setup
├── SqsIntegrationTests.cs      # SQS tests
├── EventBridgeIntegrationTests.cs
├── SnsIntegrationTests.cs
├── S3IntegrationTests.cs
└── KinesisIntegrationTests.cs
```

## Container Sharing

All tests share a single LocalStack container using xUnit's `ICollectionFixture`:

```csharp
[Collection("LocalStack")]
public class MyIntegrationTests
{
    private readonly LocalStackFixture _fixture;
    
    public MyIntegrationTests(LocalStackFixture fixture)
    {
        _fixture = fixture;
    }
}
```

This ensures the container is started once and reused across all test classes, making tests faster.

## CI/CD Integration

For CI pipelines, ensure Docker is available. Example GitHub Actions workflow:

```yaml
jobs:
  integration-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - name: Run Integration Tests
        run: |
          cd dotnet
          dotnet test test/Extensions.AWS.IntegrationTests --configuration Release
```

## Troubleshooting

### Docker Not Running
```
ERROR: Docker daemon is not running
```
Start Docker Desktop or the Docker service.

### Container Startup Timeout
If tests timeout waiting for LocalStack:
1. Check Docker has sufficient resources (2GB+ RAM recommended)
2. Try pulling the image manually: `docker pull localstack/localstack:3.0`

### Port Conflicts
Testcontainers uses random ports, so port conflicts are rare. If you see issues, check for zombie containers:
```bash
docker ps -a | grep localstack
docker rm -f <container_id>
```
