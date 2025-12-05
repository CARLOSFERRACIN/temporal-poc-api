using Temporal.POC.Api.Activities;
using Temporal.POC.Api.Config;
using Temporal.POC.Api.Workflows;
using Temporalio.Extensions.Hosting;

namespace Temporal.POC.Api.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddTemporalServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register TemporalConfig from appsettings
        services.Configure<TemporalConfig>(
            configuration.GetSection(TemporalConfig.SectionName));

        // Get TemporalConfig instance
        var temporalConfig = configuration.GetSection(TemporalConfig.SectionName).Get<TemporalConfig>()
            ?? new TemporalConfig();

        // Build the target host - check if Address already contains port
        var targetHost = temporalConfig.Address.Contains(':')
            ? temporalConfig.Address
            : $"{temporalConfig.Address}:{temporalConfig.Port}";

        // Add Temporal Client using the hosting extensions
        services.AddTemporalClient(
            clientTargetHost: targetHost,
            clientNamespace: temporalConfig.Namespace)
            .Configure(opt =>
            {
                opt.Identity = temporalConfig.Identity;
            });

        // Add Hosted Temporal Worker
        services
            .AddHostedTemporalWorker(temporalConfig.TaskQueue)
            .ConfigureOptions(opt =>
            {
                opt.MaxConcurrentWorkflowTasks = temporalConfig.Worker.MaxConcurrentWorkflowTasks;
                opt.MaxConcurrentActivities = temporalConfig.Worker.MaxConcurrentActivities;
                opt.MaxConcurrentLocalActivities = temporalConfig.Worker.MaxConcurrentLocalActivities;
                opt.MaxConcurrentActivityTaskPolls = temporalConfig.Worker.MaxConcurrentActivityTaskPolls;
                opt.MaxConcurrentWorkflowTaskPolls = temporalConfig.Worker.MaxConcurrentWorkflowTaskPolls;
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

