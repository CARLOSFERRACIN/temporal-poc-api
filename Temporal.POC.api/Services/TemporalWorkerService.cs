using Temporal.POC.Api.Activities;
using Temporal.POC.Api.Config;
using Temporal.POC.Api.Workflows;
using Temporalio.Client;
using Temporalio.Worker;
using Microsoft.Extensions.Options;

namespace Temporal.POC.Api.Services.Worker;

/// <summary>
/// Custom Hosted Service for Temporal Worker with Nexus support
/// </summary>
public class TemporalWorkerService : IHostedService
{
    private readonly ILogger<TemporalWorkerService> _logger;
    private readonly ITemporalClient _client;
    private readonly IServiceProvider _serviceProvider;
    private readonly TemporalConfig _config;
    private TemporalWorker? _worker;

    public TemporalWorkerService(
        ILogger<TemporalWorkerService> logger,
        ITemporalClient client,
        IServiceProvider serviceProvider,
        IOptions<TemporalConfig> config)
    {
        _logger = logger;
        _client = client;
        _serviceProvider = serviceProvider;
        _config = config.Value;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Temporal Worker with Nexus support on task queue: {TaskQueue}", _config.TaskQueue);

        // Build the target host
        var targetHost = _config.Address.Contains(':')
            ? _config.Address
            : $"{_config.Address}:{_config.Port}";

        // Connect to Temporal
        var connectedClient = await TemporalClient.ConnectAsync(new TemporalClientConnectOptions
        {
            TargetHost = targetHost,
            Namespace = _config.Namespace
        });

        _logger.LogInformation("Connected to Temporal at {TargetHost}", targetHost);

        // Create worker options with Nexus service
        var options = new TemporalWorkerOptions(_config.TaskQueue)
        {
            MaxConcurrentWorkflowTasks = _config.Worker.MaxConcurrentWorkflowTasks,
            MaxConcurrentActivities = _config.Worker.MaxConcurrentActivities,
            MaxConcurrentLocalActivities = _config.Worker.MaxConcurrentLocalActivities,
            MaxConcurrentActivityTaskPolls = _config.Worker.MaxConcurrentActivityTaskPolls,
            MaxConcurrentWorkflowTaskPolls = _config.Worker.MaxConcurrentWorkflowTaskPolls
        };

        // Add workflows
        options.AddWorkflow<TransactionWorkflow>();
        options.AddWorkflow<RollbackWorkflow>();
        
        // Add activities
        options.AddAllActivities(new MovementActivity());
        options.AddAllActivities(new WebhookActivity());
        
        // Add Nexus service
        options.AddNexusService(new TransactionServiceHandler());

        _logger.LogInformation("Creating Temporal Worker with Nexus service");

        // Create and start worker with connected client
        _worker = new TemporalWorker(connectedClient, options);
        
        _logger.LogInformation("Temporal Worker created successfully with Nexus service registered");

        // ExecuteAsync will keep the worker running
        _ = _worker.ExecuteAsync(cancellationToken);
        
        await Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Temporal Worker");
        
        if (_worker != null)
        {
            _worker.Dispose();
        }
        
        _logger.LogInformation("Temporal Worker stopped");
        
        await Task.CompletedTask;
    }
}

