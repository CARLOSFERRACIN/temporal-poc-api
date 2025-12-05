using System.Text.Json;
using Temporal.POC.Api.Models;
using Temporalio.Activities;

namespace Temporal.POC.Api.Activities;

public class WebhookActivity
{
    [Activity]
    public async Task<string> SendWebhookAsync(string callbackUrl, TransactionRequest request)
    {
        ActivityExecutionContext.Current.Logger.LogInformation(
            "Sending webhook to: {CallbackUrl}", callbackUrl);

        try
        {
            var payload = new
            {
                ProfileId = request.ProfileId,
                ExternalOperationId = request.ExternalOperationId,
                OperationType = request.OperationType,
                Status = "Completed",
                ProcessedAt = DateTime.UtcNow,
                MovementsCount = request.Movements.Count
            };

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            using var httpClient = new HttpClient();
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(callbackUrl, content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                ActivityExecutionContext.Current.Logger.LogInformation(
                    "Webhook sent successfully. Status: {StatusCode}, Response: {Response}",
                    response.StatusCode, responseContent);

                return $"Webhook sent successfully to {callbackUrl}. Status: {response.StatusCode}";
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                ActivityExecutionContext.Current.Logger.LogWarning(
                    "Webhook failed. Status: {StatusCode}, Response: {Response}",
                    response.StatusCode, errorContent);

                return $"Webhook failed. Status: {response.StatusCode}, Response: {errorContent}";
            }
        }
        catch (Exception ex)
        {
            ActivityExecutionContext.Current.Logger.LogError(ex,
                "Error sending webhook to: {CallbackUrl}", callbackUrl);

            throw new Exception($"Failed to send webhook to {callbackUrl}: {ex.Message}", ex);
        }
    }
}

