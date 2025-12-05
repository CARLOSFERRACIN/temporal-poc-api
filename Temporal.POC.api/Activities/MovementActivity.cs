using Temporal.POC.Api.Models;
using Temporalio.Activities;

namespace Temporal.POC.Api.Activities;

public class MovementActivity
{
    [Activity]
    public async Task<string> ProcessMovementAsync(Movement movement)
    {
        ActivityExecutionContext.Current.Logger.LogInformation(
            "Processing movement - Order: {Order}, SubOrder: {SubOrder}, TransactionType: {TransactionType}, TransactionDestination: {TransactionDestination}",
            movement.Order, movement.SubOrder, movement.TransactionType, movement.TransactionDestination);

        // Process based on transaction destination
        string result;
        switch (movement.TransactionDestination.ToLower())
        {
            case "stripe":
                result = await ProcessStripeMovement(movement);
                break;
            case "balance":
                result = await ProcessBalanceMovement(movement);
                break;
            default:
                result = $"Unknown transaction destination: {movement.TransactionDestination}";
                break;
        }

        ActivityExecutionContext.Current.Logger.LogInformation(
            "Movement processed successfully - Order: {Order}, SubOrder: {SubOrder}, Result: {Result}",
            movement.Order, movement.SubOrder, result);

        return result;
    }

    private async Task<string> ProcessStripeMovement(Movement movement)
    {
        ActivityExecutionContext.Current.Logger.LogInformation(
            "Processing Stripe movement - TransactionType: {TransactionType}, Amount: {Amount}",
            movement.TransactionType, movement.OperationTotalVl);

        // Generate a unique ID for Stripe (as mentioned in the payload example)
        var uniqueId = Guid.NewGuid().ToString().ToUpper();

        // Update Lines with generated ID if they exist
        if (movement.Lines != null && movement.Lines.Any())
        {
            foreach (var line in movement.Lines.Where(l => string.IsNullOrEmpty(l.UniqueId)))
            {
                line.UniqueId = uniqueId;
            }
        }

        return $"Stripe movement processed successfully. TransactionType: {movement.TransactionType}, Amount: {movement.OperationTotalVl}, UniqueId: {uniqueId}";
    }

    private async Task<string> ProcessBalanceMovement(Movement movement)
    {
        ActivityExecutionContext.Current.Logger.LogInformation(
            "Processing Balance movement - TransactionType: {TransactionType}, Amount: {Amount}, ProfileId: {ProfileId}",
            movement.TransactionType, movement.OperationTotalVl, movement.BalanceFields?.ProfileId);

        return $"Balance movement processed successfully. TransactionType: {movement.TransactionType}, Amount: {movement.OperationTotalVl}, ProfileId: {movement.BalanceFields?.ProfileId}";
    }
}

