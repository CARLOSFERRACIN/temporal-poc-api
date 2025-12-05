using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Temporal.POC.ExternalDomain.Api.Models;
using Temporal.POC.ExternalDomain.Api.Config;
using Temporal.POC.ExternalDomain.Api.Workflows;
using Temporalio.Client;
using Temporalio.Api.Enums.V1;

namespace Temporal.POC.ExternalDomain.Api.Controllers;

[ApiController]
[Route("v1/external-domain")]
public class ExternalDomainController : ControllerBase
{
    private readonly ITemporalClient _temporalClient;
    private readonly TemporalConfig _temporalConfig;
    private readonly ILogger<ExternalDomainController> _logger;

    public ExternalDomainController(
        ITemporalClient temporalClient, 
        IOptions<TemporalConfig> temporalConfig,
        ILogger<ExternalDomainController> logger)
    {
        _temporalClient = temporalClient;
        _temporalConfig = temporalConfig.Value;
        _logger = logger;
    }

    [HttpPost("transaction")]
    public async Task<IActionResult> ProcessTransactionViaNexus([FromBody] ExternalDomainRequest? request = null)
    {
        // Use default values if no request provided
        request ??= new ExternalDomainRequest();

        // Validate required fields
        if (request.ProfileId <= 0)
        {
            return BadRequest(new { Error = "ProfileId is required and must be greater than 0" });
        }

        if (string.IsNullOrWhiteSpace(request.ExternalOperationId))
        {
            return BadRequest(new { Error = "ExternalOperationId is required" });
        }

        if (string.IsNullOrWhiteSpace(request.OperationType))
        {
            return BadRequest(new { Error = "OperationType is required" });
        }

        // Generate workflow ID for ExternalDomainWorkflow
        var workflowId = $"external-domain-{request.OperationType}-{request.ExternalOperationId}";

        try
        {
            _logger.LogInformation(
                "Starting ExternalDomainWorkflow with ID: {WorkflowId} to call TransactionWorkflow via Nexus. ProfileId: {ProfileId}, ExternalOperationId: {ExternalOperationId}, OperationType: {OperationType}", 
                workflowId, request.ProfileId, request.ExternalOperationId, request.OperationType);

            // Parse WorkflowIdReusePolicy from config
            var policyString = _temporalConfig.WorkflowIdReusePolicy ?? "RejectDuplicate";
            var idReusePolicy = Enum.TryParse<WorkflowIdReusePolicy>(policyString, true, out var policy)
                ? policy
                : WorkflowIdReusePolicy.RejectDuplicate;

            _logger.LogInformation(
                "Starting ExternalDomainWorkflow with ID: {WorkflowId}, WorkflowIdReusePolicy: {Policy}", 
                workflowId, idReusePolicy);

            var workflowOptions = new WorkflowOptions()
            {
                Id = workflowId,
                TaskQueue = _temporalConfig.TaskQueue,
                IdReusePolicy = idReusePolicy
            };

            // Start the ExternalDomainWorkflow which will call TransactionWorkflow via Nexus
            var handle = await _temporalClient.StartWorkflowAsync(
                (ExternalDomainWorkflow wf) => wf.RunAsync(request), 
                workflowOptions);

            _logger.LogInformation(
                "ExternalDomainWorkflow started with ID: {WorkflowId}, RunId: {RunId}. This workflow will call TransactionWorkflow via Nexus.", 
                workflowId, handle.ResultRunId);

            return Accepted(new
            {
                WorkflowId = workflowId,
                RunId = handle.ResultRunId,
                Message = "ExternalDomainWorkflow started successfully. It will create TransactionRequest internally and call TransactionWorkflow via Nexus.",
                Request = new
                {
                    request.ProfileId,
                    request.ExternalOperationId,
                    request.OperationType
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting ExternalDomainWorkflow");
            return StatusCode(500, new { Error = "Internal server error", Message = ex.Message });
        }
    }
}

