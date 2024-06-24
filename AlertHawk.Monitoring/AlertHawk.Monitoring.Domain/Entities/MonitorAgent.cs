using System.Diagnostics.CodeAnalysis;

namespace AlertHawk.Monitoring.Domain.Entities;
[ExcludeFromCodeCoverage]
public class MonitorAgent
{
    public int Id { get; set; }
    public required string Hostname { get; set; }
    public required DateTime TimeStamp { get; set; }
    public bool IsMaster { get; set; }
    public int ListTasks { get; set; }
    public string? Version { get; set; }
    public MonitorRegion? MonitorRegion { get; set; }
}