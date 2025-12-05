using Temporal.POC.Api.Activities;
using Temporal.POC.Api.Models;
using Temporalio.Workflows;

namespace Temporal.POC.Api.Workflows;

[Workflow]
public class TransactionWorkflow
{
    private bool _stripeSignalReceived = false;
    private bool _stripeSuccess = false;
    private string _stripeMessage = string.Empty;

    [WorkflowRun]
    public async Task<string> RunAsync(TransactionRequest request)
    {
        Workflow.Logger.LogInformation("Starting transaction workflow for OperationType: {OperationType}, ExternalOperationId: {ExternalOperationId}",
            request.OperationType, request.ExternalOperationId);

        var results = new List<string>();
        var processedMovements = new List<Movement>();

        // Process each movement with an activity
        foreach (var movement in request.Movements.OrderBy(m => m.Order).ThenBy(m => m.SubOrder))
        {
            Workflow.Logger.LogInformation("Processing movement Order: {Order}, SubOrder: {SubOrder}, TransactionType: {TransactionType}, TransactionDestination: {TransactionDestination}",
                movement.Order, movement.SubOrder, movement.TransactionType, movement.TransactionDestination);

            var movementResult = await Workflow.ExecuteActivityAsync(
                (MovementActivity act) => act.ProcessMovementAsync(movement),
                new()
                {
                    StartToCloseTimeout = TimeSpan.FromMinutes(5),
                    RetryPolicy = new()
                    {
                        MaximumAttempts = 3
                    }
                }
            );

            results.Add(movementResult);
            processedMovements.Add(movement);

            // If this is a Stripe movement, wait for signal
            if (movement.TransactionDestination.Equals("Stripe", StringComparison.OrdinalIgnoreCase))
            {
                Workflow.Logger.LogInformation("Stripe movement processed. Waiting for signal...");

                // Reset signal state
                _stripeSignalReceived = false;
                _stripeSuccess = false;
                _stripeMessage = string.Empty;

                // Wait for signal with timeout (5 minutes)
                var signalReceived = await Workflow.WaitConditionAsync(() => _stripeSignalReceived, TimeSpan.FromMinutes(5));

                if (!signalReceived)
                {
                    Workflow.Logger.LogWarning("Stripe signal timeout. Assuming failure and reverting operation.");
                    _stripeSuccess = false;
                    _stripeMessage = "Signal timeout";
                }

                try
                {

                }
                catch (Exception)
                {
                    //start rollback
                    throw;
                }


                if (!_stripeSuccess)
                {
                    Workflow.Logger.LogWarning("Stripe operation failed. Starting rollback workflow...");

                    // Start rollback workflow
                    var rollbackWorkflowId = $"{Workflow.Info.WorkflowId}-rollback";
                    var rollbackHandle = await Workflow.StartChildWorkflowAsync(
                        (RollbackWorkflow wf) => wf.RunAsync(processedMovements, Workflow.Info.WorkflowId, _stripeMessage),
                        new()
                        {
                            Id = rollbackWorkflowId,
                            TaskQueue = "default-task-queue"
                        }
                    );

                    Workflow.Logger.LogInformation("Rollback workflow started with ID: {RollbackWorkflowId}", rollbackWorkflowId);

                    // Wait for rollback to complete
                    var rollbackResult = await rollbackHandle.GetResultAsync();

                    Workflow.Logger.LogWarning("Rollback workflow completed: {Result}", rollbackResult);

                    return $"Transaction failed. Rollback initiated: {rollbackResult}";
                }

                Workflow.Logger.LogInformation("Stripe signal received successfully. Continuing workflow...");
            }
        }

        // Send webhook callback at the end
        if (!string.IsNullOrEmpty(request.WebhookCallBackUrl))
        {
            Workflow.Logger.LogInformation("Sending webhook to: {CallbackUrl}", request.WebhookCallBackUrl);

            var webhookResult = await Workflow.ExecuteActivityAsync(
                (WebhookActivity act) => act.SendWebhookAsync(request.WebhookCallBackUrl, request),
                new()
                {
                    StartToCloseTimeout = TimeSpan.FromMinutes(2),
                    RetryPolicy = new()
                    {
                        MaximumAttempts = 3
                    }
                }
            );

            results.Add(webhookResult);
        }

        var finalResult = string.Join("; ", results);
        Workflow.Logger.LogInformation("Transaction workflow completed successfully");

        return finalResult;
    }

    [WorkflowSignal]
    public Task ReceiveStripeSignal(bool success, string message)
    {
        _stripeSignalReceived = true;
        _stripeSuccess = success;
        _stripeMessage = message;
        Workflow.Logger.LogInformation("Stripe signal received: Success={Success}, Message={Message}", success, message);
        return Task.CompletedTask;
    }

}

