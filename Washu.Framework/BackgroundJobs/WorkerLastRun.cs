namespace Washu.Framework.BackgroundJobs;

public class WorkerLastRun
{
    public string WorkerName { get; set; } = string.Empty;
    public string TaskName { get; set; } = string.Empty;
    public DateTimeOffset LastRun { get; set; }
}