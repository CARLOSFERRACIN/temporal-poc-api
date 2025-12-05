using Microsoft.AspNetCore.Mvc;
using Temporalio.Client;
using Temporal.POC.Api.Models;
using Temporal.POC.Api.Workflows;
using Temporalio.Common;

namespace Temporal.POC.Api.Controllers;

[ApiController]
[Route("v1")]
public class TransactionController : ControllerBase
{
    private readonly ITemporalClient _temporalClient;
    private readonly ILogger<TransactionController> _logger;

    public TransactionController(ITemporalClient temporalClient, ILogger<TransactionController> logger)
    {
        _temporalClient = temporalClient;
        _logger = logger;
    }

    [HttpPost("transaction")]
    public async Task<IActionResult> CreateTransaction([FromBody] TransactionRequest request)
    {
        try
        {
            if (request == null)
            {
                return BadRequest("Request body is required");
            }

            // Generate workflow ID: operation-{OperationType}-{ExternalOperationId}
            var workflowId = $"operation-{request.OperationType}-{request.ExternalOperationId}";

            _logger.LogInformation("Starting workflow with ID: {WorkflowId}", workflowId);

            // Build Search Attributes for filtering in Temporal UI
            var searchAttributes = new SearchAttributeCollection.Builder()
                .Set(SearchAttributeKey.CreateKeyword("ProfileId"), request.ProfileId.ToString())
                .Set(SearchAttributeKey.CreateKeyword("ExternalOperationId"), request.ExternalOperationId)
                .Set(SearchAttributeKey.CreateKeyword("OperationType"), request.OperationType)
                .ToSearchAttributeCollection();

            var workflowOptions = new WorkflowOptions()
            {
                Id = workflowId,
                TaskQueue = "default-task-queue",
                TypedSearchAttributes = searchAttributes
            };

            // Start the workflow with Search Attributes
            // Note: Search Attributes are already created in Temporal:
            // - ProfileId (Keyword)
            // - ExternalOperationId (Keyword)
            // - OperationType (Keyword)
            var handle = await _temporalClient.StartWorkflowAsync((TransactionWorkflow wf) => wf.RunAsync(request), workflowOptions);

            _logger.LogInformation("Workflow started with ID: {WorkflowId}, RunId: {RunId}", workflowId, handle.ResultRunId);

            return Accepted(new
            {
                WorkflowId = workflowId,
                RunId = handle.ResultRunId,
                Message = "Transaction workflow started successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting workflow");
            return StatusCode(500, new { Error = "Internal server error", Message = ex.Message });
        }
    }

    [HttpPost("webhook")]
    public IActionResult ReceiveWebhook([FromBody] object? payload)
    {
        _logger.LogInformation("Webhook received");
        return Ok(new { Message = "Webhook received" });
    }

    [HttpPost("stripe-signal")]
    public async Task<IActionResult> SendStripeSignal([FromBody] StripeSignalRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrEmpty(request.ExternalOperationId) || string.IsNullOrEmpty(request.OperationType))
            {
                return BadRequest("ExternalOperationId and OperationType are required");
            }

            // Generate workflow ID: operation-{OperationType}-{ExternalOperationId}
            var workflowId = $"operation-{request.OperationType}-{request.ExternalOperationId}";

            _logger.LogInformation("Sending signal to workflow: {WorkflowId}, Success: {Success}", workflowId, request.Success);

            // Get workflow handle
            var handle = _temporalClient.GetWorkflowHandle<TransactionWorkflow>(workflowId);

            // Send signal to workflow
            await handle.SignalAsync(wf => wf.ReceiveStripeSignal(request.Success, request.Message ?? string.Empty));

            _logger.LogInformation("Signal sent successfully to workflow: {WorkflowId}", workflowId);

            return Ok(new
            {
                WorkflowId = workflowId,
                Success = request.Success,
                Message = "Signal sent successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending signal to workflow");
            return StatusCode(500, new { Error = "Internal server error", Message = ex.Message });
        }
    }
}

