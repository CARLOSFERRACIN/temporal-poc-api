using NexusRpc;
using Temporal.POC.ExternalDomain.Api.Models;

namespace Temporal.POC.ExternalDomain.Api.Services;

/// <summary>
/// Nexus Service interface for TransactionWorkflow
/// This is the contract that allows ExternalDomain to call TransactionWorkflow via Nexus
/// This interface must match the one in the Transaction domain
/// </summary>
[NexusService]
public interface ITransactionService
{
    /// <summary>
    /// Name of the Nexus endpoint (must match the endpoint created in Temporal)
    /// </summary>
    static readonly string EndpointName = "transaction-nexus-endpoint";

    /// <summary>
    /// Nexus Operation to start a TransactionWorkflow
    /// Returns the workflow result as a string
    /// </summary>
    [NexusOperation]
    string ProcessTransaction(TransactionInput input);

    /// <summary>
    /// Input for the ProcessTransaction operation
    /// </summary>
    public record TransactionInput(TransactionRequest Request);

    /// <summary>
    /// Output for the ProcessTransaction operation
    /// </summary>
    public record TransactionOutput(string Result, string WorkflowId);
}

