using Temporal.POC.ExternalDomain.Api.Models;
using Temporal.POC.ExternalDomain.Api.Services;
using Temporalio.Workflows;

namespace Temporal.POC.ExternalDomain.Api.Workflows;

/// <summary>
/// External Domain Workflow that calls TransactionWorkflow via Nexus
/// This demonstrates cross-namespace communication using Temporal Nexus
/// </summary>
[Workflow]
public class ExternalDomainWorkflow
{
    [WorkflowRun]
    public async Task<string> RunAsync(ExternalDomainRequest request)
    {
        Workflow.Logger.LogInformation(
            "ExternalDomainWorkflow: Starting transaction via Nexus for ProfileId: {ProfileId}, ExternalOperationId: {ExternalOperationId}, OperationType: {OperationType}",
            request.ProfileId, request.ExternalOperationId, request.OperationType);

        try
        {
            // Create TransactionRequest internally based on the documentation CURL
            var transactionRequest = CreateTransactionRequest(request);

            Workflow.Logger.LogInformation(
                "ExternalDomainWorkflow: Created TransactionRequest with ExternalOperationId: {ExternalOperationId}, MovementsCount: {MovementsCount}",
                transactionRequest.ExternalOperationId, transactionRequest.Movements.Count);

            // Create Nexus client to call TransactionWorkflow in another namespace
            var nexusClient = Workflow.CreateNexusClient<ITransactionService>(
                ITransactionService.EndpointName);

            // Call the TransactionWorkflow via Nexus
            var result = await nexusClient.ExecuteNexusOperationAsync(
                svc => svc.ProcessTransaction(new ITransactionService.TransactionInput(transactionRequest)));

            Workflow.Logger.LogInformation(
                "ExternalDomainWorkflow: Transaction completed via Nexus. Result: {Result}", result);

            return $"ExternalDomainWorkflow completed. Transaction result: {result}";
        }
        catch (Exception ex)
        {
            Workflow.Logger.LogError(ex, 
                "ExternalDomainWorkflow: Error calling TransactionWorkflow via Nexus");
            throw;
        }
    }

    private TransactionRequest CreateTransactionRequest(ExternalDomainRequest request)
    {
        var operationUuid = Guid.NewGuid().ToString().ToUpper();
        var transactionUuid = Guid.NewGuid().ToString().ToUpper();
        var operationDt = Workflow.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");

        return new TransactionRequest
        {
            ProfileId = request.ProfileId,
            ExternalOperationId = request.ExternalOperationId,
            OperationType = request.OperationType,
            AppCallerNm = "external-domain-api",
            WebhookCallBackUrl = "http://temporal-poc-api:8080/v1/webhook",
            Movements = new List<Movement>
            {
                new Movement
                {
                    Order = 1,
                    SubOrder = 1,
                    OperationUuid = operationUuid,
                    TransactionUuid = transactionUuid,
                    TransactionDestination = "Stripe",
                    TransactionType = "ChargeStripeAccount",
                    ExternalId = "RadiusMailOrderId - 10000",
                    OperationDt = operationDt,
                    OperationTotalVl = 79.75m,
                    StripeFields = new StripeFields
                    {
                        PartnerProfileId = "XXXX",
                        ExtrasFields1 = "ExtrasFields2"
                    },
                    Lines = new List<Line>
                    {
                        new Line
                        {
                            LineType = "Integration",
                            LineVl = 79.75m,
                            UniqueId = ""
                        }
                    }
                },
                new Movement
                {
                    Order = 2,
                    SubOrder = 1,
                    OperationUuid = operationUuid,
                    TransactionUuid = transactionUuid,
                    TransactionDestination = "Balance",
                    TransactionType = "CreditStripeFunds",
                    ExternalId = "RadiusMailOrderId - 10000",
                    OperationDt = operationDt,
                    OperationTotalVl = 79.75m,
                    BalanceFields = new BalanceFields
                    {
                        ProfileId = request.ProfileId
                    }
                },
                new Movement
                {
                    Order = 2,
                    SubOrder = 2,
                    OperationUuid = operationUuid,
                    TransactionUuid = transactionUuid,
                    TransactionDestination = "Balance",
                    TransactionType = "ChargeStripeAccount",
                    ExternalId = "RadiusMailOrderId - 10000",
                    OperationDt = operationDt,
                    OperationTotalVl = -79.75m,
                    BalanceFields = new BalanceFields
                    {
                        ProfileId = request.ProfileId
                    },
                    Lines = new List<Line>
                    {
                        new Line
                        {
                            LineType = "LargeHandwrittenCardA8",
                            LineVl = -70.75m,
                            UniqueId = ""
                        },
                        new Line
                        {
                            LineType = "FirstClassPostage",
                            LineVl = -6.25m,
                            UniqueId = ""
                        },
                        new Line
                        {
                            LineType = "RecipientData",
                            LineVl = -2.75m,
                            UniqueId = ""
                        }
                    }
                }
            }
        };
    }
}

