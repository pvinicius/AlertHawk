using AlertHawk.Monitoring.Domain.Entities;
using Monitor = AlertHawk.Monitoring.Domain.Entities.Monitor;

namespace AlertHawk.Monitoring.Domain.Interfaces.Services;

public interface IMonitorService
{
    Task<IEnumerable<MonitorNotification>> GetMonitorNotifications(int id);
    Task<IEnumerable<MonitorHistory>> GetMonitorHistory(int id);
    Task<IEnumerable<Monitor>> GetMonitorList();
}