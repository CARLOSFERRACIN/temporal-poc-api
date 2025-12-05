# Testing Commands

Complete guide for testing the Temporal POC API.

## 🚀 Starting Services

### Start All Services (First Time)

```bash
docker-compose up -d --build
```

This will:
- Build the API Docker image
- Pull required images (Temporal, PostgreSQL, UI)
- Start all containers in detached mode
- Wait for health checks to pass

**Wait 30-60 seconds** for all services to be ready.

### Verify Services Status

```bash
docker-compose ps
```

Expected output:
```
NAME                  STATUS
temporal-poc-api       Up
temporal              Up (healthy)
temporal-postgresql   Up (healthy)
temporal-ui           Up
```

## 🔄 Refresh After Code Changes

### Rebuild and Restart All Services

```bash
docker-compose down
docker-compose up -d --build
```

### Rebuild Only API (Faster)

```bash
docker-compose up -d --build api
```

### Restart API Without Rebuild

```bash
docker-compose restart api
```

### View API Logs in Real-Time

```bash
docker logs -f temporal-poc-api
```

## 📡 Testing the API

### Basic Transaction Request (Postman-compatible cURL)

```bash
curl -X POST "http://localhost:5000/v1/transaction" \
  -H "Content-Type: application/json" \
  -d '{
    "ProfileId": 1010,
    "ExternalOperationId": "OP-12345-TEST",
    "OperationType": "RadiusMailOrder",
    "AppCallerNm": "postman",
    "WebhookCallBackUrl": "https://webhook.site/unique-id-here",
    "Movements": [
      {
        "Order": 1,
        "SubOrder": 1,
        "OperationUuid": "543D19A8-BB30-4D52-8B77-DF2E9E3ADDCC",
        "TransactionUuid": "543D19A8-BB30-4D52-8B77-DF2E9E3ADDCC",
        "TransactionDestination": "Stripe",
        "TransactionType": "ChargeStripeAccount",
        "ExternalId": "RadiusMailOrderId - 10000",
        "OperationDt": "2025-12-23 11:18:30.053",
        "OperationTotalVl": 79.75,
        "StripeFields": {
          "PartnerProfileId": "XXXX",
          "ExtrasFields1": "ExtrasFields2"
        },
        "Lines": [
          {
            "LineType": "Integration",
            "LineVl": 79.75,
            "UniqueId": ""
          }
        ]
      },
      {
        "Order": 2,
        "SubOrder": 1,
        "OperationUuid": "543D19A8-BB30-4D52-8B77-DF2E9E3ADDCC",
        "TransactionUuid": "543D19A8-BB30-4D52-8B77-DF2E9E3ADDCC",
        "TransactionDestination": "Balance",
        "TransactionType": "CreditStripeFunds",
        "ExternalId": "RadiusMailOrderId - 10000",
        "OperationDt": "2025-12-23 11:18:30.053",
        "OperationTotalVl": 79.75,
        "BalanceFields": {
          "ProfileId": 1010
        }
      },
      {
        "Order": 2,
        "SubOrder": 2,
        "OperationUuid": "543D19A8-BB30-4D52-8B77-DF2E9E3ADDCC",
        "TransactionUuid": "543D19A8-BB30-4D52-8B77-DF2E9E3ADDCC",
        "TransactionDestination": "Balance",
        "TransactionType": "ChargeStripeAccount",
        "ExternalId": "RadiusMailOrderId - 10000",
        "OperationDt": "2025-12-23 11:18:30.053",
        "OperationTotalVl": -79.75,
        "BalanceFields": {
          "ProfileId": 1010
        },
        "Lines": [
          {
            "LineType": "LargeHandwrittenCardA8",
            "LineVl": -70.75,
            "UniqueId": ""
          },
          {
            "LineType": "FirstClassPostage",
            "LineVl": -6.25,
            "UniqueId": ""
          },
          {
            "LineType": "RecipientData",
            "LineVl": -2.75,
            "UniqueId": ""
          }
        ]
      }
    ]
  }'
```


**Expected Response:**
```json
{
  "workflowId": "operation-RadiusMailOrder-OP-12345-TEST",
  "runId": "019aeeaa-eead-730c-a359-f1c6ba13640c",
  "message": "Transaction workflow started successfully"
}
```

> **Note:** For information about accessing and using the Temporal UI, see [README.md](README.md#-accessing-temporal-ui).

## 📊 Monitoring

### View API Logs

```bash
# Follow logs in real-time
docker logs -f temporal-poc-api

# Last 50 lines
docker logs temporal-poc-api --tail 50

# Last 100 lines with timestamps
docker logs temporal-poc-api --tail 100 -t
```

### View Temporal Server Logs

```bash
docker logs -f temporal
```

### View All Service Logs

```bash
docker-compose logs -f
```

### View Specific Service Logs

```bash
docker-compose logs -f api
docker-compose logs -f temporal
docker-compose logs -f postgresql
```

## 🧪 Testing Webhook Callback

The workflow sends a webhook to the `WebhookCallBackUrl` specified in the transaction request after all movements are processed.

### 📍 Webhook URL Configuration

**When running in Docker**, use the correct URL based on where the call originates:

- **From inside Docker (workflow calling itself)**: 
  - Use: `http://temporal-poc-api:8080/v1/webhook`
  - This uses the container name and internal port (8080)

- **From host machine (external calls)**:
  - Use: `http://localhost:5000/v1/webhook`
  - This uses the mapped port (5000)

- **From another container in the same network**:
  - Use: `http://temporal-poc-api:8080/v1/webhook`
  - Container name resolves within Docker network

### 🧪 Testing Options

1. **Test with webhook.site** (recommended for testing):
   - Visit https://webhook.site
   - Copy your unique URL
   - Use this URL in `WebhookCallBackUrl` field

2. **Test with internal webhook endpoint**:
   - Use: `http://temporal-poc-api:8080/v1/webhook` (when running in Docker)
   - Use: `http://localhost:5000/v1/webhook` (when running locally)

3. **Check webhook.site** after the workflow completes:
   - You should see a POST request with the completion status
   - Payload includes:
     - `profileId`
     - `externalOperationId`
     - `operationType`
     - `status` (Completed)
     - `processedAt`
     - `movementsCount`

## 🎯 Testing Complete Flow: Success and Failure Scenarios

The API supports a workflow that waits for a Stripe signal after processing Stripe movements. This section explains how to test both success and failure scenarios using Postman-compatible curl commands.

### 📋 Understanding the Flow

1. **Start Transaction**: POST `/v1/transaction` - Starts the workflow
2. **Stripe Processing**: Workflow processes Stripe movement and waits for signal
3. **Send Signal**: POST `/v1/stripe-signal` - Sends success or failure signal
4. **Result**:
   - **Success**: Workflow continues processing remaining movements
   - **Failure**: Rollback workflow is started to revert all processed movements

### ✅ Test Scenario 1: Success Flow

This test demonstrates a successful transaction where the Stripe payment is confirmed.

#### Step 1: Start the Transaction

Copy this curl command to Postman (Import > Raw Text):

```bash
curl -X POST "http://localhost:5000/v1/transaction" \
  -H "Content-Type: application/json" \
  -d '{
    "ProfileId": 1010,
    "ExternalOperationId": "OP-SUCCESS-TEST",
    "OperationType": "RadiusMailOrder",
    "AppCallerNm": "test-success",
    "WebhookCallBackUrl": "http://temporal-poc-api:8080/v1/webhook",
    "Movements": [
      {
        "Order": 1,
        "SubOrder": 1,
        "OperationUuid": "543D19A8-BB30-4D52-8B77-DF2E9E3ADDCC",
        "TransactionUuid": "543D19A8-BB30-4D52-8B77-DF2E9E3ADDCC",
        "TransactionDestination": "Stripe",
        "TransactionType": "ChargeStripeAccount",
        "ExternalId": "RadiusMailOrderId - 10000",
        "OperationDt": "2025-12-23 11:18:30.053",
        "OperationTotalVl": 79.75,
        "StripeFields": {
          "PartnerProfileId": "XXXX",
          "ExtrasFields1": "ExtrasFields2"
        },
        "Lines": [
          {
            "LineType": "Integration",
            "LineVl": 79.75,
            "UniqueId": ""
          }
        ]
      },
      {
        "Order": 2,
        "SubOrder": 1,
        "OperationUuid": "543D19A8-BB30-4D52-8B77-DF2E9E3ADDCC",
        "TransactionUuid": "543D19A8-BB30-4D52-8B77-DF2E9E3ADDCC",
        "TransactionDestination": "Balance",
        "TransactionType": "CreditStripeFunds",
        "ExternalId": "RadiusMailOrderId - 10000",
        "OperationDt": "2025-12-23 11:18:30.053",
        "OperationTotalVl": 79.75,
        "BalanceFields": {
          "ProfileId": 1010
        }
      }
    ]
  }'
```

**Expected Response:**
```json
{
  "workflowId": "operation-RadiusMailOrder-OP-SUCCESS-TEST",
  "runId": "019aeeaa-eead-730c-a359-f1c6ba13640c",
  "message": "Transaction workflow started successfully"
}
```

#### Step 2: Wait a Few Seconds

Wait 2-3 seconds for the Stripe movement to be processed. The workflow will now be waiting for a signal.

#### Step 3: Send Success Signal

Copy this curl command to Postman:

```bash
curl -X POST "http://localhost:5000/v1/stripe-signal" \
  -H "Content-Type: application/json" \
  -d '{
    "ExternalOperationId": "OP-SUCCESS-TEST",
    "OperationType": "RadiusMailOrder",
    "Success": true,
    "Message": "Stripe payment confirmed successfully"
  }'
```

**Expected Response:**
```json
{
  "workflowId": "operation-RadiusMailOrder-OP-SUCCESS-TEST",
  "success": true,
  "message": "Signal sent successfully"
}
```

#### Step 4: Verify Success

- The workflow will continue processing remaining movements
- Final webhook will be sent
- Workflow will complete successfully
- Check Temporal UI at http://localhost:8080 to see the completed workflow

### ❌ Test Scenario 2: Failure Flow with Rollback

This test demonstrates a failed transaction where the Stripe payment fails, triggering a rollback workflow.

#### Step 1: Start the Transaction

Copy this curl command to Postman:

```bash
curl -X POST "http://localhost:5000/v1/transaction" \
  -H "Content-Type: application/json" \
  -d '{
    "ProfileId": 1010,
    "ExternalOperationId": "OP-FAILURE-TEST",
    "OperationType": "RadiusMailOrder",
    "AppCallerNm": "test-failure",
    "WebhookCallBackUrl": "http://temporal-poc-api:8080/v1/webhook",
    "Movements": [
      {
        "Order": 1,
        "SubOrder": 1,
        "OperationUuid": "543D19A8-BB30-4D52-8B77-DF2E9E3ADDCC",
        "TransactionUuid": "543D19A8-BB30-4D52-8B77-DF2E9E3ADDCC",
        "TransactionDestination": "Stripe",
        "TransactionType": "ChargeStripeAccount",
        "ExternalId": "RadiusMailOrderId - 10000",
        "OperationDt": "2025-12-23 11:18:30.053",
        "OperationTotalVl": 79.75,
        "StripeFields": {
          "PartnerProfileId": "XXXX",
          "ExtrasFields1": "ExtrasFields2"
        },
        "Lines": [
          {
            "LineType": "Integration",
            "LineVl": 79.75,
            "UniqueId": ""
          }
        ]
      },
      {
        "Order": 2,
        "SubOrder": 1,
        "OperationUuid": "543D19A8-BB30-4D52-8B77-DF2E9E3ADDCC",
        "TransactionUuid": "543D19A8-BB30-4D52-8B77-DF2E9E3ADDCC",
        "TransactionDestination": "Balance",
        "TransactionType": "CreditStripeFunds",
        "ExternalId": "RadiusMailOrderId - 10000",
        "OperationDt": "2025-12-23 11:18:30.053",
        "OperationTotalVl": 79.75,
        "BalanceFields": {
          "ProfileId": 1010
        }
      }
    ]
  }'
```

**Expected Response:**
```json
{
  "workflowId": "operation-RadiusMailOrder-OP-FAILURE-TEST",
  "runId": "019aeeaa-eead-730c-a359-f1c6ba13640c",
  "message": "Transaction workflow started successfully"
}
```

#### Step 2: Wait a Few Seconds

Wait 2-3 seconds for the Stripe movement to be processed. The workflow will now be waiting for a signal.

#### Step 3: Send Failure Signal

Copy this curl command to Postman:

```bash
curl -X POST "http://localhost:5000/v1/stripe-signal" \
  -H "Content-Type: application/json" \
  -d '{
    "ExternalOperationId": "OP-FAILURE-TEST",
    "OperationType": "RadiusMailOrder",
    "Success": false,
    "Message": "Stripe payment failed - insufficient funds"
  }'
```

**Expected Response:**
```json
{
  "workflowId": "operation-RadiusMailOrder-OP-FAILURE-TEST",
  "success": false,
  "message": "Signal sent successfully"
}
```

#### Step 4: Verify Rollback

- The original workflow will detect the failure
- A rollback workflow will be started with ID: `operation-RadiusMailOrder-OP-FAILURE-TEST-rollback`
- The rollback workflow will revert all processed movements in reverse order
- Check Temporal UI at http://localhost:8080 to see:
  - Original workflow (failed)
  - Rollback workflow (completed)

### 📊 Monitoring the Tests

#### View Workflow Logs

```bash
# Follow API logs in real-time
docker logs -f temporal-poc-api

# Filter for specific test
docker logs temporal-poc-api | grep "OP-SUCCESS-TEST"
docker logs temporal-poc-api | grep "OP-FAILURE-TEST"
```

#### Check Temporal UI

1. Open http://localhost:8080
2. Search for workflow IDs:
   - `operation-RadiusMailOrder-OP-SUCCESS-TEST` (should be Completed)
   - `operation-RadiusMailOrder-OP-FAILURE-TEST` (should be Completed with failure)
   - `operation-RadiusMailOrder-OP-FAILURE-TEST-rollback` (should be Completed)

#### Expected Log Messages

**Success Flow:**
- "Stripe movement processed. Waiting for signal..."
- "Stripe signal received: Success=True"
- "Stripe signal received successfully. Continuing workflow..."
- "Transaction workflow completed successfully"

**Failure Flow:**
- "Stripe movement processed. Waiting for signal..."
- "Stripe signal received: Success=False"
- "Stripe operation failed. Starting rollback workflow..."
- "Rollback workflow started with ID: ...-rollback"
- "Starting rollback workflow for original workflow: ..."
- "Reverting movement Order: X, SubOrder: Y"
- "Rollback workflow completed"

### 📝 Endpoint Reference

#### POST /v1/transaction
Starts a new transaction workflow.

**cURL (Postman-compatible):**
```bash
curl -X POST "http://localhost:5000/v1/transaction" \
  -H "Content-Type: application/json" \
  -d '{
    "ProfileId": 1010,
    "ExternalOperationId": "OP-12345",
    "OperationType": "RadiusMailOrder",
    "AppCallerNm": "postman",
    "WebhookCallBackUrl": "http://temporal-poc-api:8080/v1/webhook",
    "Movements": [...]
  }'
```

**Request Body:**
- `ProfileId` (int): Profile identifier
- `ExternalOperationId` (string): Unique operation ID
- `OperationType` (string): Type of operation (e.g., "RadiusMailOrder")
- `AppCallerNm` (string): Application caller name
- `WebhookCallBackUrl` (string): URL for final webhook callback
- `Movements` (array): List of movements to process

#### POST /v1/stripe-signal
Sends a signal to a waiting workflow after Stripe processing.

**cURL (Postman-compatible):**
```bash
curl -X POST "http://localhost:5000/v1/stripe-signal" \
  -H "Content-Type: application/json" \
  -d '{
    "ExternalOperationId": "OP-12345",
    "OperationType": "RadiusMailOrder",
    "Success": true,
    "Message": "Optional message"
  }'
```

**Request Body:**
- `ExternalOperationId` (string, required): Must match the transaction's ExternalOperationId
- `OperationType` (string, required): Must match the transaction's OperationType
- `Success` (boolean, required): `true` to continue, `false` to trigger rollback
- `Message` (string, optional): Additional message about the result

#### POST /v1/webhook
Receives webhook callbacks (logs only).

**cURL (Postman-compatible):**
```bash
curl -X POST "http://localhost:5000/v1/webhook" \
  -H "Content-Type: application/json" \
  -d '{
    "any": "payload"
  }'
```

**Request Body:** Any JSON payload

**Response:**
```json
{
  "message": "Webhook received"
}
```

## 🛑 Stopping Services

### Stop All Services

```bash
docker-compose down
```

### Stop and Remove Volumes (Clean Slate)

```bash
docker-compose down -v
```

**Warning**: This removes all data including workflow history.

### Stop Specific Service

```bash
docker-compose stop api
docker-compose stop temporal
```

## 🔧 Troubleshooting

### API Not Responding

1. Check if API container is running:
   ```bash
   docker-compose ps api
   ```

2. Check API logs:
   ```bash
   docker logs temporal-poc-api
   ```

3. Restart API:
   ```bash
   docker-compose restart api
   ```

### Temporal Not Connecting

1. Check Temporal health:
   ```bash
   docker-compose ps temporal
   ```

2. Check Temporal logs:
   ```bash
   docker logs temporal
   ```

3. Verify network connectivity:
   ```bash
   docker exec tempora-poc-api ping -c 2 temporal
   ```

### Workflow Not Appearing in UI

1. Wait a few seconds (workflow registration takes time)
2. Refresh the Temporal UI page
3. Check the correct namespace (`default`)
4. Verify workflow ID format
5. Check Temporal server logs for errors

### Build Errors

1. Clean build cache:
   ```bash
   docker-compose build --no-cache
   ```

2. Remove old images:
   ```bash
   docker system prune -a
   ```

3. Rebuild from scratch:
   ```bash
   docker-compose down -v
   docker-compose up -d --build
   ```

## 📝 Quick Reference

| Command | Description |
|---------|-------------|
| `docker-compose up -d --build` | Start all services |
| `docker-compose down` | Stop all services |
| `docker-compose up -d --build api` | Rebuild and restart API only |
| `docker-compose restart api` | Restart API without rebuild |
| `docker logs -f temporal-poc-api` | Follow API logs |
| `docker-compose ps` | Check service status |
| `docker-compose logs -f` | Follow all logs |

## 🔗 Useful Links

- **Temporal UI**: http://localhost:8080
- **API**: http://localhost:5000
- **Webhook Testing**: https://webhook.site
- **Temporal Docs**: https://docs.temporal.io
