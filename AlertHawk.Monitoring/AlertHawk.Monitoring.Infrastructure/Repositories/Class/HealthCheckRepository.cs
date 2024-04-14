using System.Data;
using System.Data.SqlClient;
using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using Dapper;
using Microsoft.Extensions.Configuration;

namespace AlertHawk.Monitoring.Infrastructure.Repositories.Class;

public class HealthCheckRepository : RepositoryBase, IHealthCheckRepository
{
    private readonly string _connstring;

    public HealthCheckRepository(IConfiguration configuration) : base(configuration)
    {
        _connstring = GetConnectionString();
    }

    public async Task<bool> CheckHealthAsync()
    {
        try
        {
            await using var db = new SqlConnection(_connstring);
            string sqlAllMonitors = @"SELECT TOP 1 Id FROM [MonitorAgent]";
            var result = await db.QueryFirstOrDefaultAsync<MonitorAgent>(sqlAllMonitors, commandType: CommandType.Text);
           
            return result != null;
        }
        catch (Exception e)
        {
            SentrySdk.CaptureException(e);
            return false;
        }
    }
}