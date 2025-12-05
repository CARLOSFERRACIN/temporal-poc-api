using NexusRpc.Handlers;
using Temporal.POC.Api.Models;
using Temporal.POC.Api.Services;
using Temporal.POC.Api.Workflows;
using Temporalio.Nexus;
using Temporalio.Client;

namespace Temporal.POC.Api.Services;

/// <summary>
/// Nexus Service Handler for ITransactionService
/// Exposes TransactionWorkflow as a Nexus Operation
/// </summary>
[NexusServiceHandler(typeof(ITransactionService))]
public class TransactionServiceHandler
{
    /// <summary>
    /// Handler for ProcessTransaction operation
    /// Starts TransactionWorkflow asynchronously via Nexus
    /// </summary>
    [NexusOperationHandler]
    public IOperationHandler<ITransactionService.TransactionInput, string> ProcessTransaction() =>
        // This Nexus service operation is backed by a workflow run
        WorkflowRunOperationHandler.FromHandleFactory(
            (WorkflowRunOperationContext context, ITransactionService.TransactionInput input) =>
            {
                // Generate workflow ID: operation-{OperationType}-{ExternalOperationId}
                var workflowId = $"operation-{input.Request.OperationType}-{input.Request.ExternalOperationId}";

                // Start the TransactionWorkflow
                return context.StartWorkflowAsync(
                    (TransactionWorkflow wf) => wf.RunAsync(input.Request),
                    // Workflow IDs should typically be business meaningful IDs and are used to
                    // dedupe workflow starts. For this example, we're using the request ID
                    // allocated by Temporal when the caller workflow schedules the operation,
                    // this ID is guaranteed to be stable across retries of this operation.
                    new() 
                    { 
                        Id = workflowId,
                        TaskQueue = "default-task-queue"
                    });
            });
}

