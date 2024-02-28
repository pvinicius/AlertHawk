using AlertHawk.Monitoring.Domain.Entities;

namespace AlertHawk.Monitoring.Domain.Interfaces.Repositories;

public interface IMonitorGroupRepository
{
    Task<IEnumerable<MonitorGroup>> GetMonitorGroupList();
    Task<MonitorGroup> GetMonitorGroupById(int id);
    Task AddMonitorToGroup(MonitorGroupItems monitorGroupItems);
    Task RemoveMonitorFromGroup(MonitorGroupItems monitorGroupItems);
    Task AddMonitorGroup(MonitorGroup monitorGroup);
    Task UpdateMonitorGroup(MonitorGroup monitorGroup);
    Task DeleteMonitorGroup(int id);
}