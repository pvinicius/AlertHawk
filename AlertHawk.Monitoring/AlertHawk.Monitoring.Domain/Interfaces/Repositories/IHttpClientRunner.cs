using AlertHawk.Monitoring.Domain.Entities;

namespace AlertHawk.Monitoring.Domain.Interfaces.Repositories;

public interface IHttpClientRunner
{
    Task<MonitorHttp> CheckUrlsAsync(MonitorHttp monitorHttp);
}