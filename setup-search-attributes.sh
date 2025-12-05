#!/bin/bash
# Script to create Search Attributes in Temporal
# Run this from bash

echo "Creating Search Attributes in Temporal..."

# Create ProfileId as Keyword (with auto-confirmation)
docker exec temporal sh -c "echo Y | tctl --address temporal:7233 admin cluster add-search-attributes --name ProfileId --type Keyword"

# Create ExternalOperationId as Keyword (with auto-confirmation)
docker exec temporal sh -c "echo Y | tctl --address temporal:7233 admin cluster add-search-attributes --name ExternalOperationId --type Keyword"

# Create OperationType as Keyword (with auto-confirmation)
docker exec temporal sh -c "echo Y | tctl --address temporal:7233 admin cluster add-search-attributes --name OperationType --type Keyword"

echo "Search Attributes created successfully!"
echo "ProfileId (Keyword)"
echo "ExternalOperationId (Keyword)"
echo "OperationType (Keyword)"

