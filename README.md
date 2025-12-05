# Temporal POC API

.NET Core 9 API with Temporal.io for transaction processing with idempotency guarantee.

## 📋 Requirements

- .NET 9 SDK
- Docker and Docker Compose

## 🚀 Quick Start

### Start All Services with Docker Compose

The entire application runs in Docker containers, including the API, Temporal server, PostgreSQL, and Temporal UI.

```bash
docker-compose up -d --build
```

This command will:
- **Build the API Docker images** - Compiles .NET 9 applications
  - `temporal-poc-api` - Main API with TransactionWorkflow
  - `temporal-poc-external-domain-api` - External API with Nexus integration
- **Start PostgreSQL database** - Temporal's persistence layer
- **Start Temporal server** - Workflow orchestration engine
  - Loads dynamic configuration from `config/dynamicconfig/docker.yaml`
  - Enables Nexus operations
  - Configures callback endpoints
- **Start Temporal UI** - Web interface for monitoring workflows
- **Initialize Search Attributes** - Creates custom search fields
  - ProfileId (Keyword)
  - ExternalOperationId (Keyword)
  - OperationType (Keyword)
- **Create Nexus Endpoint** - Sets up cross-namespace communication
  - Endpoint: `transaction-nexus-endpoint`
  - Target namespace: `default`
  - Target task queue: `default-task-queue`

**Wait 30-60 seconds** for all services to initialize completely.

### Verify Services are Running

```bash
docker-compose ps
```

You should see all containers with status `Up` or `Up (healthy)`:
- `temporal-poc-api` - Main API service (port 5000)
- `temporal-poc-external-domain-api` - External API with Nexus (port 6000)
- `temporal` - Temporal server (port 7233)
- `temporal-postgresql` - PostgreSQL database (port 5432)
- `temporal-ui` - Temporal web UI (port 8080)
- `temporal-init-search-attributes` - Exited (ran once to create search attributes)
- `temporal-init-nexus-endpoint` - Exited (ran once to create Nexus endpoint)

### Access Points

Once all services are running:

- **Main API**: `http://localhost:5000`
- **External Domain API (Nexus)**: `http://localhost:6000`
- **Temporal UI**: `http://localhost:8080`
- **Temporal Server (gRPC)**: `localhost:7233`
- **PostgreSQL**: `localhost:5432`
- **Swagger UI**: Available when running locally with `dotnet run` at `https://localhost:5001/swagger`

## 🔄 Refresh Docker After Code Changes

After making code changes, you need to rebuild and restart the containers:

### Option 1: Rebuild and Restart All Services

```bash
docker-compose down
docker-compose up -d --build
```

### Option 2: Rebuild Only the API (Faster)

```bash
docker-compose up -d --build api
```

### Option 3: Restart Without Rebuild (If Only Config Changed)

```bash
docker-compose restart api
```

### View API Logs

```bash
# Follow API logs in real-time
docker logs -f temporal-poc-api

# View last 50 lines
docker logs temporal-poc-api --tail 50
```

## 🖥️ Accessing Temporal UI

The Temporal UI provides a web interface to monitor and manage workflows.

### Access the UI

1. Open your browser and navigate to: **http://localhost:8080**

2. You'll see the Temporal UI dashboard with:
   - **Workflows** tab - View all workflows
   - **Namespaces** - Default namespace is `default`
   - **Task Queues** - See `default-task-queue`

### View Your Workflow

1. In the **Workflows** tab, search for your workflow ID:
   - Format: `operation-{OperationType}-{ExternalOperationId}`
   - Example: `operation-RadiusMailOrder-OP-12345-TEST`

2. Click on the workflow to see:
   - **Workflow Execution Details**
   - **Activities** executed (one per movement + webhook)
   - **Timeline** of execution
   - **Input/Output** of each activity
   - **Logs** and error details
   - **Retry attempts** if any

3. **Filter Options**:
   - Filter by Workflow ID
   - Filter by Status (Running, Completed, Failed)
   - Filter by Time Range

### Workflow Details View

When you click on a workflow, you can see:
- **Execution Time** - How long the workflow took
- **Activities List** - All activities executed in order
- **Activity Details** - Click any activity to see:
  - Input parameters
  - Output result
  - Execution time
  - Retry history
  - Error messages (if any)

### Useful Temporal UI Features

- **Replay Workflow** - Re-execute a workflow with the same input
- **Terminate Workflow** - Stop a running workflow
- **Cancel Workflow** - Cancel a scheduled workflow
- **View History** - See complete execution history

## 🔄 Temporal Workflow

### Characteristics:
- **Workflow ID**: `operation-{OperationType}-{ExternalOperationId}`
  - Ensures idempotency (same operation won't execute twice)
  
- **Task Queue**: `default-task-queue`

- **Processing Flow**:
  1. Orders movements by `Order` and `SubOrder`
  2. Creates an **Activity** for each movement
  3. Processes movements sequentially:
     - **Stripe**: Simulates Stripe integration, generates UniqueId
     - **Balance**: Simulates balance operation
  4. **Stripe Signal**: After Stripe movement, workflow waits for signal
     - **Success**: Continues processing remaining movements
     - **Failure**: Starts rollback workflow to revert all processed movements
  5. At the end, sends **Webhook** to `WebhookCallBackUrl`

### Activities:

#### MovementActivity
- Processes each movement based on `TransactionDestination`
- Supports: Stripe, Balance
- Automatic retry (up to 3 attempts)
- Timeout: 5 minutes

#### WebhookActivity
- Sends HTTP POST callback with operation status
- Automatic retry (up to 3 attempts)
- Timeout: 2 minutes

## 🏗️ Project Architecture

```
Temporal.POC.api/
├── Controllers/
│   └── TransactionController.cs      # POST /v1/transaction, /v1/stripe-signal, /v1/webhook
├── Models/
│   ├── TransactionRequest.cs         # Request DTOs
│   └── StripeSignalRequest.cs         # Signal request DTO
├── Workflows/
│   ├── TransactionWorkflow.cs        # Main Temporal workflow
│   └── RollbackWorkflow.cs            # Rollback workflow
├── Activities/
│   ├── MovementActivity.cs            # Movement processing
│   └── WebhookActivity.cs            # Webhook callback
├── Extensions/
│   └── ServiceExtensions.cs           # IoC container configuration
├── Dockerfile                         # API Docker image
├── docker-compose.yml                 # All services orchestration
└── README.md                          # This file
```

## 🛠️ Technologies Used

- **.NET 9** - Framework
- **Temporalio** - Workflow engine SDK
- **Temporal.io** - Durable workflow orchestration
- **Docker** - Containerization
- **PostgreSQL** - Temporal database
- **Swagger/OpenAPI** - API documentation

## 📦 Docker Compose Services

### Running Services
- **api**: Main .NET 9 API (port 5000)
  - Hosts TransactionWorkflow and RollbackWorkflow
  - Exposes Nexus Service Handler for cross-namespace calls
  - Includes custom TemporalWorkerService for Nexus support
- **external-domain-api**: External .NET 9 API (port 6000)
  - Demonstrates cross-namespace communication
  - Calls TransactionWorkflow via Nexus
  - Independent project with ExternalDomainWorkflow
- **temporal**: Temporal Server (port 7233)
  - Workflow orchestration engine
  - Uses dynamic configuration for Nexus
  - Connects to PostgreSQL for persistence
- **temporal-ui**: Temporal Web UI (port 8080)
  - Visual interface for monitoring workflows
  - Browse workflow history and execution details
- **postgresql**: PostgreSQL database (port 5432)
  - Stores workflow history and state
  - Temporal's persistence layer

### Init Services (Run Once)
- **init-search-attributes**: Creates custom search attributes
  - Runs on startup, exits when complete
  - Creates ProfileId, ExternalOperationId, OperationType fields
- **init-nexus-endpoint**: Creates Nexus endpoint
  - Runs on startup, exits when complete
  - Creates `transaction-nexus-endpoint` for cross-namespace calls

## ⚙️ Configuration

Configuration can be changed in `appsettings.json`:

```json
{
  "Temporal": {
    "Address": "temporal:7233"
  }
}
```

Note: When running in Docker, use `temporal:7233` (service name). When running locally, use `localhost:7233`.

## 🛑 Stop Services

### Stop Containers (Keep Data)

```bash
docker-compose down
```

This will stop and remove containers but keep volumes (data persists).

### Stop and Remove All Data

```bash
docker-compose down -v
```

This will stop containers and remove volumes (all workflow history will be deleted).

### Clean Everything and Start Fresh

If you want to completely clean Docker and rebuild from scratch with fresh images:

```bash
# Step 1: Stop all containers and remove volumes
docker-compose down -v

# Step 2: Remove all Docker images, containers, and build cache
docker system prune -a --volumes -f

# Step 3: Rebuild and start everything from scratch
docker-compose up -d --build
```

**What gets deleted:**
- All containers
- All Docker images (Temporal, PostgreSQL, APIs, UI)
- All volumes (workflow history, database data)
- All build cache
- All networks

**What happens on rebuild:**
- Fresh download of Temporal and PostgreSQL images
- Clean compilation of .NET applications
- New database without any workflow history
- Fresh Nexus endpoint creation
- New search attributes setup

**Note**: This is useful when:
- You want to test with a completely clean environment
- You've made significant infrastructure changes
- You're troubleshooting persistent Docker issues
- You want to remove all traces of old workflows

## 📊 View Logs

```bash
# API logs
docker logs -f temporal-poc-api

# Temporal server logs
docker logs -f temporal

# PostgreSQL logs
docker logs -f temporal-postgresql

# All services logs
docker-compose logs -f
```

## 🔍 Search Attributes

The workflow automatically includes Search Attributes for filtering in Temporal UI:
- **ProfileId** (Keyword) - Profile identifier
- **ExternalOperationId** (Keyword) - External operation ID
- **OperationType** (Keyword) - Operation type

### Automatic Setup

**Search Attributes are automatically created when you start the services with `docker-compose up`.**

The `init-search-attributes` service runs automatically after Temporal is ready and creates all required Search Attributes. You don't need to run any manual setup commands.

### Manual Setup (Optional)

If you need to recreate Search Attributes manually, you can use the provided scripts:

**Windows PowerShell:**
```powershell
.\setup-search-attributes.ps1
```

**Linux/Mac/Bash:**
```bash
chmod +x setup-search-attributes.sh
./setup-search-attributes.sh
```

After setup, workflows will automatically include these Search Attributes, allowing you to filter workflows in Temporal UI by ProfileId, ExternalOperationId, or OperationType.

## 🧪 Testing

For complete testing instructions, examples, and curl commands, see **[TEST-COMMANDS.md](TEST-COMMANDS.md)**.

The test documentation includes:
- How to test success and failure scenarios
- Complete curl commands for Postman
- How to send signals to workflows
- How to monitor workflows in Temporal UI

## 🔒 Temporal Guarantees

- **Durability**: Workflow state persists across failures
- **Idempotency**: Unique WorkflowId prevents duplication
- **Automatic Retry**: Activities with configurable retry policy
- **Timeout Protection**: Protection against stuck processes
- **Observability**: Logs and metrics via Temporal UI

## 🐛 Troubleshooting

### API not connecting to Temporal

1. Check if Temporal is healthy:
   ```bash
   docker-compose ps
   ```

2. Check Temporal logs:
   ```bash
   docker logs temporal
   ```

3. Verify API can reach Temporal:
   ```bash
   docker exec temporal-poc-api ping -c 2 temporal
   ```

### Workflow not appearing in UI

1. Wait a few seconds for the workflow to be registered
2. Refresh the Temporal UI page
3. Check the correct namespace (`default`)
4. Verify the workflow ID format matches

### Container build fails

1. Clean Docker build cache:
   ```bash
   docker-compose build --no-cache
   ```

2. Remove old images:
   ```bash
   docker system prune -a
   ```
