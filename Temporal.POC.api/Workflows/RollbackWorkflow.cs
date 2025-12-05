using Temporal.POC.Api.Activities;
using Temporal.POC.Api.Models;
using Temporalio.Workflows;

namespace Temporal.POC.Api.Workflows;

[Workflow]
public class RollbackWorkflow
{
    [WorkflowRun]
    public async Task<string> RunAsync(List<Movement> movementsToRevert, string originalWorkflowId, string reason)
    {
        Workflow.Logger.LogInformation("Starting rollback workflow for original workflow: {OriginalWorkflowId}, Reason: {Reason}, Movements to revert: {Count}",
            originalWorkflowId, reason, movementsToRevert.Count);

        var revertResults = new List<string>();

        // Revert movements in reverse order (descending by Order, then SubOrder)
        foreach (var movement in movementsToRevert.OrderByDescending(m => m.Order).ThenByDescending(m => m.SubOrder))
        {
            Workflow.Logger.LogInformation("Reverting movement Order: {Order}, SubOrder: {SubOrder}, TransactionType: {TransactionType}, TransactionDestination: {TransactionDestination}",
                movement.Order, movement.SubOrder, movement.TransactionType, movement.TransactionDestination);

            // Create a revert movement (opposite operation)
            var revertMovement = new Movement
            {
                Order = movement.Order,
                SubOrder = movement.SubOrder,
                OperationUuid = movement.OperationUuid,
                TransactionUuid = movement.TransactionUuid,
                TransactionDestination = movement.TransactionDestination,
                TransactionType = GetRevertTransactionType(movement.TransactionType),
                ExternalId = movement.ExternalId,
                OperationDt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                OperationTotalVl = -movement.OperationTotalVl, // Reverse the amount
                StripeFields = movement.StripeFields,
                BalanceFields = movement.BalanceFields,
                Lines = movement.Lines?.Select(l => new Line
                {
                    LineType = l.LineType,
                    LineVl = -l.LineVl, // Reverse the line amount
                    UniqueId = l.UniqueId
                }).ToList()
            };

            try
            {
                var revertResult = await Workflow.ExecuteActivityAsync(
                    (MovementActivity act) => act.ProcessMovementAsync(revertMovement),
                    new()
                    {
                        StartToCloseTimeout = TimeSpan.FromMinutes(5),
                        RetryPolicy = new()
                        {
                            MaximumAttempts = 3
                        }
                    }
                );

                revertResults.Add($"Order {movement.Order}, SubOrder {movement.SubOrder}: {revertResult}");
                Workflow.Logger.LogInformation("Movement reverted successfully: Order {Order}, SubOrder {SubOrder}", movement.Order, movement.SubOrder);
            }
            catch (Exception ex)
            {
                Workflow.Logger.LogError(ex, "Error reverting movement Order: {Order}, SubOrder: {SubOrder}",
                    movement.Order, movement.SubOrder);
                revertResults.Add($"Order {movement.Order}, SubOrder {movement.SubOrder}: REVERT FAILED - {ex.Message}");
            }
        }

        var revertSummary = string.Join("; ", revertResults);
        Workflow.Logger.LogInformation("Rollback workflow completed. Summary: {Summary}", revertSummary);

        return $"Rollback completed. Reason: {reason}. Results: {revertSummary}";
    }

    private string GetRevertTransactionType(string originalType)
    {
        // Map transaction types to their revert operations
        return originalType switch
        {
            "ChargeStripeAccount" => "RefundStripeAccount",
            "CreditStripeFunds" => "DebitStripeFunds",
            "DebitStripeFunds" => "CreditStripeFunds",
            "RefundStripeAccount" => "ChargeStripeAccount",
            _ => $"Revert{originalType}"
        };
    }
}

