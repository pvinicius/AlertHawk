using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using AlertHawk.Monitoring.Domain.Interfaces.Services;

namespace AlertHawk.Monitoring.Domain.Classes;

public class MonitorAgentService: IMonitorAgentService
{
    private IMonitorAgentRepository _monitorAgentRepository;

    public MonitorAgentService(IMonitorAgentRepository monitorAgentRepository)
    {
        _monitorAgentRepository = monitorAgentRepository;
    }

    public async Task<IEnumerable<MonitorAgent>> GetAllMonitorAgents()
    {
        var agents = await _monitorAgentRepository.GetAllMonitorAgents();
        var agentTasks = await _monitorAgentRepository.GetAllMonitorAgentTasks();
        
        foreach (var agent in agents)
        {
            agent.ListTasks = agentTasks.Count(x => x.MonitorAgentId == agent.Id);
        }

        return agents;
    }
}