namespace Temporal.POC.ExternalDomain.Api.Config;

public class TemporalConfig
{
    public const string SectionName = "Temporal";

    public string Address { get; set; } = "temporal";
    public int Port { get; set; } = 7233;
    public string Namespace { get; set; } = "external-domain-namespace";
    public string Identity { get; set; } = "Temporal.POC.ExternalDomain.api";
    public string TaskQueue { get; set; } = "external-domain-task-queue";
    public string WorkflowIdReusePolicy { get; set; } = "RejectDuplicate"; // Options: AllowDuplicate, AllowDuplicateFailedOnly, RejectDuplicate
    
    public WorkerConfig Worker { get; set; } = new();
    
    public class WorkerConfig
    {
        public int MaxConcurrentWorkflowTasks { get; set; } = 100;
        public int MaxConcurrentActivities { get; set; } = 100;
        public int MaxConcurrentLocalActivities { get; set; } = 100;
        public int MaxConcurrentActivityTaskPolls { get; set; } = 20;
        public int MaxConcurrentWorkflowTaskPolls { get; set; } = 20;
    }
}

