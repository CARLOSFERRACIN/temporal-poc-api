using System.Text.Json.Serialization;

namespace Temporal.POC.ExternalDomain.Api.Models;

/// <summary>
/// Transaction Request model - shared contract for Nexus communication
/// This should match the contract in the Transaction domain
/// </summary>
public class TransactionRequest
{
    [JsonPropertyName("ProfileId")]
    public int ProfileId { get; set; }

    [JsonPropertyName("ExternalOperationId")]
    public string ExternalOperationId { get; set; } = string.Empty;

    [JsonPropertyName("OperationType")]
    public string OperationType { get; set; } = string.Empty;

    [JsonPropertyName("AppCallerNm")]
    public string AppCallerNm { get; set; } = string.Empty;

    [JsonPropertyName("WebhookCallBackUrl")]
    public string WebhookCallBackUrl { get; set; } = string.Empty;

    [JsonPropertyName("Movements")]
    public List<Movement> Movements { get; set; } = new();
}

public class Movement
{
    [JsonPropertyName("Order")]
    public int Order { get; set; }

    [JsonPropertyName("SubOrder")]
    public int SubOrder { get; set; }

    [JsonPropertyName("OperationUuid")]
    public string OperationUuid { get; set; } = string.Empty;

    [JsonPropertyName("TransactionUuid")]
    public string TransactionUuid { get; set; } = string.Empty;

    [JsonPropertyName("TransactionDestination")]
    public string TransactionDestination { get; set; } = string.Empty;

    [JsonPropertyName("TransactionType")]
    public string TransactionType { get; set; } = string.Empty;

    [JsonPropertyName("ExternalId")]
    public string ExternalId { get; set; } = string.Empty;

    [JsonPropertyName("OperationDt")]
    public string OperationDt { get; set; } = string.Empty;

    [JsonPropertyName("OperationTotalVl")]
    public decimal OperationTotalVl { get; set; }

    [JsonPropertyName("StripeFields")]
    public StripeFields? StripeFields { get; set; }

    [JsonPropertyName("BalanceFields")]
    public BalanceFields? BalanceFields { get; set; }

    [JsonPropertyName("Lines")]
    public List<Line>? Lines { get; set; }
}

public class StripeFields
{
    [JsonPropertyName("PartnerProfileId")]
    public string PartnerProfileId { get; set; } = string.Empty;

    [JsonPropertyName("ExtrasFields1")]
    public string ExtrasFields1 { get; set; } = string.Empty;
}

public class BalanceFields
{
    [JsonPropertyName("ProfileId")]
    public int ProfileId { get; set; }
}

public class Line
{
    [JsonPropertyName("LineType")]
    public string LineType { get; set; } = string.Empty;

    [JsonPropertyName("LineVl")]
    public decimal LineVl { get; set; }

    [JsonPropertyName("UniqueId")]
    public string UniqueId { get; set; } = string.Empty;
}

