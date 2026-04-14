#!/bin/bash
set -e

echo "Setting up AWS services in LocalStack..."

ENDPOINT="${LOCALSTACK_ENDPOINT:-http://localhost:4566}"

export AWS_ACCESS_KEY_ID="test"
export AWS_SECRET_ACCESS_KEY="test"
export AWS_DEFAULT_REGION="us-east-1"

invoke_aws() {
    aws --endpoint-url "$ENDPOINT" "$@"
}

# SQS
echo ""
echo "[SQS] Creating queues..."
invoke_aws sqs create-queue --queue-name "test-queue" > /dev/null
invoke_aws sqs create-queue --queue-name "s3-events-queue" > /dev/null
invoke_aws sqs create-queue --queue-name "eventbridge-target-queue" > /dev/null

# SNS
echo "[SNS] Creating topic..."
TOPIC_ARN=$(invoke_aws sns create-topic --name "test-topic" --output json | jq -r '.TopicArn')

# Subscribe SQS to SNS for testing
echo "[SNS] Subscribing SQS queue to topic..."
invoke_aws sns subscribe \
    --topic-arn "$TOPIC_ARN" \
    --protocol sqs \
    --notification-endpoint "arn:aws:sqs:us-east-1:000000000000:test-queue" > /dev/null

# EventBridge
echo "[EventBridge] Creating event bus..."
invoke_aws events create-event-bus --name "test-bus" 2>/dev/null || echo "  Event bus may already exist"

invoke_aws events put-rule \
    --name "catch-all" \
    --event-bus-name "test-bus" \
    --event-pattern '{"source":[{"prefix":""}]}' > /dev/null

invoke_aws events put-targets \
    --rule "catch-all" \
    --event-bus-name "test-bus" \
    --targets "Id=sqs-target,Arn=arn:aws:sqs:us-east-1:000000000000:eventbridge-target-queue" > /dev/null

# S3
echo "[S3] Creating bucket..."
invoke_aws s3 mb "s3://test-bucket" 2>/dev/null || echo "  Bucket may already exist"

# Configure S3 to send events to SQS
cat > /tmp/s3-notification.json << 'EOF'
{
    "QueueConfigurations": [
        {
            "QueueArn": "arn:aws:sqs:us-east-1:000000000000:s3-events-queue",
            "Events": ["s3:ObjectCreated:*", "s3:ObjectRemoved:*"]
        }
    ]
}
EOF

invoke_aws s3api put-bucket-notification-configuration \
    --bucket "test-bucket" \
    --notification-configuration file:///tmp/s3-notification.json

rm /tmp/s3-notification.json

# Kinesis
echo "[Kinesis] Creating stream..."
invoke_aws kinesis create-stream --stream-name "test-stream" --shard-count 1 2>/dev/null || echo "  Stream may already exist"
echo "  Waiting for stream to become active..."
sleep 5

echo ""
echo "✅ Setup complete!"
echo ""
echo "Resources created:"
echo "  SQS Queues:"
echo "    - http://localhost:4566/000000000000/test-queue"
echo "    - http://localhost:4566/000000000000/s3-events-queue"
echo "    - http://localhost:4566/000000000000/eventbridge-target-queue"
echo ""
echo "  SNS Topic:"
echo "    - arn:aws:sns:us-east-1:000000000000:test-topic"
echo ""
echo "  EventBridge:"
echo "    - Event Bus: test-bus (with catch-all rule -> SQS)"
echo ""
echo "  S3:"
echo "    - Bucket: test-bucket (with notification -> SQS)"
echo ""
echo "  Kinesis:"
echo "    - Stream: test-stream (1 shard)"
echo ""
echo "Environment variables for local.settings.json:"
echo "  AWS_ACCESS_KEY_ID=test"
echo "  AWS_SECRET_ACCESS_KEY=test"
echo "  AWS_REGION=us-east-1"
echo "  SQS_QUEUE_URL=http://localhost:4566/000000000000/test-queue"
echo "  SNS_TOPIC_ARN=arn:aws:sns:us-east-1:000000000000:test-topic"
echo "  EVENT_BUS_NAME=test-bus"
echo "  S3_BUCKET_NAME=test-bucket"
echo "  KINESIS_STREAM_NAME=test-stream"
echo "  *_SERVICE_URL=http://localhost:4566"
