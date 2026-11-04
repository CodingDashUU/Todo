namespace Washu.Framework.Identity.Entities;

public class WorkerLastRun
{
    public string WorkerName { get; set; } = string.Empty;
    public DateTimeOffset LastRun { get; set; }
}