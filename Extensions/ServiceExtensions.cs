using Temporal.POC.Api.Activities;
using Temporal.POC.Api.Workflows;
using Temporalio.Extensions.Hosting;

namespace Temporal.POC.Api.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddTemporalServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Get Temporal address from configuration (use service name in Docker, localhost when running locally)
        var temporalAddress = configuration.GetValue<string>("Temporal:Address") ?? "temporal:7233";
        
        // Extract host and port from address
        var parts = temporalAddress.Split(':');
        var host = parts[0];
        var port = parts.Length > 1 ? int.Parse(parts[1]) : 7233;

        // Add Temporal Client using the hosting extensions
        services.AddTemporalClient(
            clientTargetHost: $"{host}:{port}",
            clientNamespace: "default")
            .Configure(opt =>
            {
                opt.Identity = "Temporal.POC.api";
            });

        // Add Hosted Temporal Worker
        services
            .AddHostedTemporalWorker("default-task-queue")
            .ConfigureOptions(opt =>
            {
                opt.MaxConcurrentWorkflowTasks = 100;
                opt.MaxConcurrentActivities = 100;
                opt.MaxConcurrentLocalActivities = 100;
                opt.MaxConcurrentActivityTaskPolls = 20;
                opt.MaxConcurrentWorkflowTaskPolls = 20;
            })
            .AddScopedActivities<MovementActivity>()
            .AddScopedActivities<WebhookActivity>()
            .AddWorkflow<TransactionWorkflow>()
            .AddWorkflow<RollbackWorkflow>();

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register controllers
        services.AddControllers();

        // Register Swagger
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new()
            {
                Title = "Temporal POC API",
                Version = "v1",
                Description = "API para processamento de transações com Temporal.io"
            });
        });

        return services;
    }
}

