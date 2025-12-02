#!/bin/bash

# Send Test Message to LocalStack SQS
# This script sends a test message to the LocalStack SQS queue for testing Azure Functions triggers

set -e

QUEUE_NAME=${1:-test-queue}
ENDPOINT_URL=${AWS_ENDPOINT_URL:-http://localhost:4566}
REGION=${AWS_REGION:-us-east-1}

echo "📤 Sending test message to queue: $QUEUE_NAME"

# Get queue URL
QUEUE_URL=$(aws --endpoint-url=$ENDPOINT_URL sqs get-queue-url \
    --queue-name $QUEUE_NAME \
    --region $REGION \
    --output text \
    --no-cli-pager)

if [ -z "$QUEUE_URL" ]; then
    echo "❌ Error: Could not find queue '$QUEUE_NAME'"
    echo "   Make sure LocalStack is running: ./setup-localstack.sh"
    exit 1
fi

echo "   Queue URL: $QUEUE_URL"

# Generate test message
TIMESTAMP=$(date -u +"%Y-%m-%dT%H:%M:%SZ")
MESSAGE_BODY=$(cat <<EOF
{
  "id": "$(uuidgen)",
  "timestamp": "$TIMESTAMP",
  "type": "test",
  "message": "Hello from LocalStack SQS!",
  "data": {
    "source": "send-test-message.sh",
    "environment": "local"
  }
}
EOF
)

# Send message
MESSAGE_ID=$(aws --endpoint-url=$ENDPOINT_URL sqs send-message \
    --queue-url $QUEUE_URL \
    --message-body "$MESSAGE_BODY" \
    --region $REGION \
    --output text \
    --query 'MessageId' \
    --no-cli-pager)

echo "✅ Message sent successfully!"
echo "   Message ID: $MESSAGE_ID"
echo ""
echo "📋 Message body:"
echo "$MESSAGE_BODY"
echo ""
echo "🔍 Check your Azure Function logs to see the trigger response"
