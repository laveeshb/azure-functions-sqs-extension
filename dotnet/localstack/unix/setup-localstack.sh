#!/bin/bash

# LocalStack Setup Script for Azure Functions SQS Extension Testing
# This script sets up LocalStack with SQS queues for local testing

set -e

echo "🚀 Starting LocalStack for SQS testing..."

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo "❌ Error: Docker is not running. Please start Docker first."
    exit 1
fi

# Start LocalStack
echo "📦 Starting LocalStack container..."
docker-compose -f "$(dirname "$0")/docker-compose.localstack.yml" up -d

# Wait for LocalStack to be ready
echo "⏳ Waiting for LocalStack to be ready..."
timeout=30
counter=0
until curl -s http://localhost:4566/_localstack/health | grep -q '"sqs": "available"' || [ $counter -eq $timeout ]; do
    echo "   Waiting... ($counter/$timeout seconds)"
    sleep 1
    ((counter++))
done

if [ $counter -eq $timeout ]; then
    echo "❌ Error: LocalStack failed to start within $timeout seconds"
    exit 1
fi

echo "✅ LocalStack is ready!"

# Create test queues
echo "📝 Creating SQS test queues..."

# Input queue
aws --endpoint-url=http://localhost:4566 sqs create-queue \
    --queue-name test-queue \
    --region us-east-1 \
    --no-cli-pager \
    > /dev/null 2>&1 && echo "   ✓ Created 'test-queue'" || echo "   ℹ Queue 'test-queue' already exists"

# Output queue
aws --endpoint-url=http://localhost:4566 sqs create-queue \
    --queue-name test-output-queue \
    --region us-east-1 \
    --no-cli-pager \
    > /dev/null 2>&1 && echo "   ✓ Created 'test-output-queue'" || echo "   ℹ Queue 'test-output-queue' already exists"

# FIFO queue
aws --endpoint-url=http://localhost:4566 sqs create-queue \
    --queue-name test-queue.fifo \
    --attributes FifoQueue=true \
    --region us-east-1 \
    --no-cli-pager \
    > /dev/null 2>&1 && echo "   ✓ Created 'test-queue.fifo'" || echo "   ℹ Queue 'test-queue.fifo' already exists"

# Dead letter queue
aws --endpoint-url=http://localhost:4566 sqs create-queue \
    --queue-name test-dlq \
    --region us-east-1 \
    --no-cli-pager \
    > /dev/null 2>&1 && echo "   ✓ Created 'test-dlq'" || echo "   ℹ Queue 'test-dlq' already exists"

echo ""
echo "✅ LocalStack SQS setup complete!"
echo ""
echo "📋 Available queues:"
aws --endpoint-url=http://localhost:4566 sqs list-queues --region us-east-1 --no-cli-pager
echo ""
echo "🔗 LocalStack endpoint: http://localhost:4566"
echo "🌍 Region: us-east-1"
echo ""
echo "⚙️  Configure your local.settings.json with:"
echo '{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "AWS_REGION": "us-east-1",
    "AWS_ACCESS_KEY_ID": "test",
    "AWS_SECRET_ACCESS_KEY": "test",
    "AWS_ENDPOINT_URL": "http://localhost:4566"
  }
}'
echo ""
echo "🧪 To send a test message:"
echo "   ./localstack/send-test-message.sh"
echo ""
echo "🛑 To stop LocalStack:"
echo "   docker-compose -f localstack/docker-compose.localstack.yml down"
