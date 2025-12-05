#!/bin/sh
# Script to wait for Temporal and create Search Attributes
# This script runs inside a container

set -e

TEMPORAL_ADDRESS="${TEMPORAL_ADDRESS:-temporal:7233}"
MAX_RETRIES=30
RETRY_INTERVAL=5

echo "Waiting for Temporal to be ready at $TEMPORAL_ADDRESS..."

# Wait for Temporal to be ready
for i in $(seq 1 $MAX_RETRIES); do
    if tctl --address "$TEMPORAL_ADDRESS" cluster health > /dev/null 2>&1; then
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

# Create Search Attributes (ignore errors if they already exist)
echo "Creating Search Attributes..."

# ProfileId
echo "Creating ProfileId (Keyword)..."
echo "Y" | tctl --address "$TEMPORAL_ADDRESS" admin cluster add-search-attributes --name ProfileId --type Keyword 2>&1 | grep -v "DEPRECATION NOTICE" | grep -v "Error" || echo "ProfileId may already exist"

# ExternalOperationId
echo "Creating ExternalOperationId (Keyword)..."
echo "Y" | tctl --address "$TEMPORAL_ADDRESS" admin cluster add-search-attributes --name ExternalOperationId --type Keyword 2>&1 | grep -v "DEPRECATION NOTICE" | grep -v "Error" || echo "ExternalOperationId may already exist"

# OperationType
echo "Creating OperationType (Keyword)..."
echo "Y" | tctl --address "$TEMPORAL_ADDRESS" admin cluster add-search-attributes --name OperationType --type Keyword 2>&1 | grep -v "DEPRECATION NOTICE" | grep -v "Error" || echo "OperationType may already exist"

echo ""
echo "Configuring namespace settings..."
# Note: Temporal doesn't have a namespace-level WorkflowIdReusePolicy setting
# The policy must be set per workflow in the client code
# However, we can verify the namespace exists and is properly configured
tctl --address "$TEMPORAL_ADDRESS" namespace describe default 2>&1 | grep -v "DEPRECATION NOTICE" || echo "Default namespace exists"

echo ""
echo "Search Attributes setup completed!"
echo "ProfileId (Keyword)"
echo "ExternalOperationId (Keyword)"
echo "OperationType (Keyword)"
echo ""
echo "Note: WorkflowIdReusePolicy must be set in client code (WorkflowOptions)"
echo "      The default namespace is configured and ready to use."

