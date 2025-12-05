using System.Text.Json.Serialization;

namespace Temporal.POC.Api.Models;

public class StripeSignalRequest
{
    [JsonPropertyName("ExternalOperationId")]
    public string ExternalOperationId { get; set; } = string.Empty;

    [JsonPropertyName("OperationType")]
    public string OperationType { get; set; } = string.Empty;

    [JsonPropertyName("Success")]
    public bool Success { get; set; }

    [JsonPropertyName("Message")]
    public string? Message { get; set; }
}

