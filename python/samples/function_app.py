"""Sample Azure Functions app using SQS extension.

This sample demonstrates how to use the azure-functions-sqs package
to create Azure Functions that trigger from and send messages to
Amazon SQS queues.
"""

import json
import logging
from datetime import timedelta

import azure.functions as func

from azure_functions_sqs import (
    SqsCollector,
    SqsMessage,
    SqsOutput,
    SqsOutputOptions,
    SqsTrigger,
    SqsTriggerOptions,
)

app = func.FunctionApp()

# =============================================================================
# SQS Trigger Example
# =============================================================================

# Simple trigger that processes messages from an SQS queue
trigger = SqsTrigger(
    queue_url="%SQS_QUEUE_URL%",  # Reads from environment variable
    region="%AWS_REGION%",  # Optional: extracted from URL if not specified
    aws_key_id="%AWS_ACCESS_KEY_ID%",
    aws_access_key="%AWS_SECRET_ACCESS_KEY%",
    options=SqsTriggerOptions(
        max_number_of_messages=10,  # Batch size (1-10)
        visibility_timeout=timedelta(seconds=30),  # Message lock timeout
        polling_interval=timedelta(seconds=5),  # Delay between polls when queue is empty
    ),
)


@trigger
def process_sqs_message(message: SqsMessage) -> None:
    """
    Process a single message from SQS.

    After successful processing, the message is automatically deleted.
    If an exception is raised, the message remains in the queue and
    will be retried after the visibility timeout.
    """
    logging.info(f"Processing message: {message.message_id}")
    logging.info(f"Body: {message.body}")

    # Access message attributes
    if message.message_attributes:
        for name, attr in message.message_attributes.items():
            logging.info(f"Attribute {name}: {attr.string_value}")

    # Process the message with error handling
    try:
        data = json.loads(message.body)
        logging.info(f"Processed order: {data.get('order_id')}")
    except json.JSONDecodeError as e:
        logging.error(f"Failed to parse message body as JSON: {e}")
        raise


# =============================================================================
# SQS Output Binding Example
# =============================================================================

# Output binding for sending messages to SQS
output = SqsOutput(
    queue_url="%OUTPUT_QUEUE_URL%",
    aws_key_id="%AWS_ACCESS_KEY_ID%",
    aws_access_key="%AWS_SECRET_ACCESS_KEY%",
)


@app.function_name("SendToSqs")
@app.route(route="send", methods=["POST"])
@output
def send_to_sqs(req: func.HttpRequest) -> str:
    """
    HTTP-triggered function that sends a message to SQS.

    The return value is automatically sent to the SQS queue
    configured in the @output decorator.
    """
    body = req.get_json()
    if body is None:
        return json.dumps({"error": "Request body must be valid JSON"})

    # The return value becomes the SQS message body
    return json.dumps({
        "timestamp": body.get("timestamp"),
        "data": body.get("data"),
        "source": "azure-function",
    })


# =============================================================================
# SQS Collector Example (Batch Sending)
# =============================================================================

@app.function_name("BatchSendToSqs")
@app.route(route="batch", methods=["POST"])
def batch_send_to_sqs(req: func.HttpRequest) -> func.HttpResponse:
    """
    HTTP-triggered function that sends multiple messages to SQS.

    Uses SqsCollector for efficient batch sending (similar to
    IAsyncCollector in .NET).
    """
    # Create a collector for batch sending
    collector = SqsCollector(
        queue_url="%OUTPUT_QUEUE_URL%",
        aws_key_id="%AWS_ACCESS_KEY_ID%",
        aws_access_key="%AWS_SECRET_ACCESS_KEY%",
    )

    body = req.get_json()
    if body is None:
        return func.HttpResponse(
            json.dumps({"error": "Request body must be valid JSON"}),
            mimetype="application/json",
            status_code=400,
        )

    items = body.get("items", [])

    # Add multiple messages
    for item in items:
        collector.add({
            "item_id": item.get("id"),
            "action": item.get("action", "process"),
        })

    # Flush sends all messages in batches of 10
    sent_count = collector.flush()

    return func.HttpResponse(
        json.dumps({"messages_sent": sent_count}),
        mimetype="application/json",
        status_code=200,
    )


# =============================================================================
# FIFO Queue Example
# =============================================================================

fifo_output = SqsOutput(
    queue_url="%FIFO_QUEUE_URL%",  # Must end with .fifo
    aws_key_id="%AWS_ACCESS_KEY_ID%",
    aws_access_key="%AWS_SECRET_ACCESS_KEY%",
    options=SqsOutputOptions(message_group_id="default-group"),  # Required for FIFO queues
)


@app.function_name("SendToFifoQueue")
@app.route(route="fifo", methods=["POST"])
@fifo_output
def send_to_fifo_queue(req: func.HttpRequest) -> str:
    """
    Send ordered messages to a FIFO queue.
    """
    body = req.get_json()
    if body is None:
        return json.dumps({"error": "Request body must be valid JSON"})
    return json.dumps(body)


# =============================================================================
# Main entry point (for local development)
# =============================================================================

if __name__ == "__main__":
    # Start the trigger polling loop for local testing
    import asyncio

    async def run():
        await trigger.start_async()
        try:
            # Keep running until interrupted
            while True:
                await asyncio.sleep(1)
        except KeyboardInterrupt:
            await trigger.stop_async()

    asyncio.run(run())
