#!/bin/sh
# Script to create Nexus Endpoint for TransactionWorkflow
# This script runs inside a container

set -e

TEMPORAL_ADDRESS="${TEMPORAL_ADDRESS:-temporal:7233}"
MAX_RETRIES=30
RETRY_INTERVAL=5

echo "Waiting for Temporal to be ready at $TEMPORAL_ADDRESS..."

# Wait for Temporal to be ready
for i in $(seq 1 $MAX_RETRIES); do
    if temporal --address "$TEMPORAL_ADDRESS" operator cluster health > /dev/null 2>&1; then
        echo "Temporal is ready!"
        break
    fi
    if [ $i -eq $MAX_RETRIES ]; then
        echo "Temporal did not become ready after $MAX_RETRIES attempts"
        exit 1
    fi
    echo "Waiting for Temporal... ($i/$MAX_RETRIES)"
    sleep $RETRY_INTERVAL
done

# Wait a bit more to ensure Temporal is fully ready
sleep 3

# Create Nexus Endpoint (ignore errors if it already exists)
echo "Creating Nexus Endpoint: transaction-nexus-endpoint..."
temporal operator nexus endpoint create \
  --address "$TEMPORAL_ADDRESS" \
  --name transaction-nexus-endpoint \
  --target-namespace default \
  --target-task-queue default-task-queue \
  2>&1 || echo "Nexus endpoint may already exist"

echo ""
echo "Nexus Endpoint setup completed!"
echo "Endpoint: transaction-nexus-endpoint"
echo "Target Namespace: default"
echo "Target Task Queue: default-task-queue"

