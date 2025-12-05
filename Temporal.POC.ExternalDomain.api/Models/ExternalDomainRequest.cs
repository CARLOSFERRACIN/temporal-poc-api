namespace Temporal.POC.ExternalDomain.Api.Models;

/// <summary>
/// Simple request model for ExternalDomain API
/// The workflow will create the full TransactionRequest internally
/// </summary>
public class ExternalDomainRequest
{
    public int ProfileId { get; set; }
    public string ExternalOperationId { get; set; } = string.Empty;
    public string OperationType { get; set; } = "RadiusMailOrder";
}

