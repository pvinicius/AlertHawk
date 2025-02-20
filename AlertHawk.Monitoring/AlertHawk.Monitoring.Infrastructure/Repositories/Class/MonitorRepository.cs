using AlertHawk.Monitoring.Domain.Entities;
using AlertHawk.Monitoring.Domain.Interfaces.Repositories;
using Dapper;
using Hangfire;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using Monitor = AlertHawk.Monitoring.Domain.Entities.Monitor;

namespace AlertHawk.Monitoring.Infrastructure.Repositories.Class;

[ExcludeFromCodeCoverage]
public class MonitorRepository : RepositoryBase, IMonitorRepository
{
    private readonly string _connstring;

    public MonitorRepository(IConfiguration configuration) : base(configuration)
    {
        _connstring = GetConnectionString();
    }

    public async Task<IEnumerable<Monitor?>> GetMonitorList()
    {
        await using var db = new SqlConnection(_connstring);
        string sql =
            @"SELECT Id, Name, MonitorTypeId, HeartBeatInterval, Retries, Status, DaysToExpireCert, Paused, MonitorRegion, MonitorEnvironment FROM [Monitor]";
        return await db.QueryAsync<Monitor>(sql, commandType: CommandType.Text);
    }

    public async Task<IEnumerable<Monitor?>> GetMonitorRunningList()
    {
        await using var db = new SqlConnection(_connstring);
        string sql =
            @"SELECT Id, Name, MonitorTypeId, HeartBeatInterval, Retries, Status, DaysToExpireCert, Paused, MonitorRegion, MonitorEnvironment FROM [Monitor] WHERE Paused = 0";
        return await db.QueryAsync<Monitor>(sql, commandType: CommandType.Text);
    }

    public async Task<IEnumerable<Monitor?>> GetFullMonitorList()
    {
        await using var db = new SqlConnection(_connstring);
        string sql =
            @"SELECT M.Id, M.Name, HTTP.UrlToCheck, CAST(IP AS VARCHAR(255)) + ':' + CAST(Port AS VARCHAR(10)) AS MonitorTcp, MonitorTypeId, HeartBeatInterval, Retries, Status, DaysToExpireCert, Paused, MonitorRegion, MonitorEnvironment, Tag, HTTP.CheckCertExpiry FROM [Monitor] M
                LEFT JOIN MonitorHttp HTTP on HTTP.MonitorId = M.Id
                LEFT JOIN MonitorTcp TCP ON TCP.MonitorId = M.Id";
        return await db.QueryAsync<Monitor>(sql, new { }, commandType: CommandType.Text);
    }

    public async Task<IEnumerable<MonitorHttp>> GetMonitorHttpList()
    {
        await using var db = new SqlConnection(_connstring);
        string sql = "SELECT MonitorId, CheckCertExpiry, IgnoreTlsSsl, MaxRedirects, UrlToCheck, Timeout, MonitorHttpMethod, Body, HeadersJson FROM [MonitorHttp]";
        return await db.QueryAsync<MonitorHttp>(sql, commandType: CommandType.Text);
    }

    public async Task<IEnumerable<MonitorTcp>> GetMonitorTcpList()
    {
        await using var db = new SqlConnection(_connstring);
        string sql = "SELECT MonitorId, Port, IP, Timeout, LastStatus FROM [MonitorTcp]";
        return await db.QueryAsync<MonitorTcp>(sql, commandType: CommandType.Text);
    }

    public async Task<int> CreateMonitor(Monitor monitor)
    {
        await using var db = new SqlConnection(_connstring);
        string sqlMonitor =
            @"INSERT INTO [Monitor] (Name, MonitorTypeId, HeartBeatInterval, Retries, Status, DaysToExpireCert, Paused, MonitorRegion, MonitorEnvironment, Tag)
            VALUES (@Name, @MonitorTypeId, @HeartBeatInterval, @Retries, @Status, @DaysToExpireCert, @Paused, @MonitorRegion, @MonitorEnvironment, @Tag); SELECT CAST(SCOPE_IDENTITY() as int)";
        var id = await db.ExecuteScalarAsync<int>(sqlMonitor,
            new
            {
                monitor.Name,
                monitor.MonitorTypeId,
                monitor.HeartBeatInterval,
                monitor.Retries,
                monitor.Status,
                monitor.DaysToExpireCert,
                monitor.Paused,
                monitor.MonitorRegion,
                monitor.MonitorEnvironment,
                monitor.Tag
            }, commandType: CommandType.Text);
        return id;
    }

    public async Task WipeMonitorData()
    {
        await using var db = new SqlConnection(_connstring);
        var sqlMonitor = "TRUNCATE TABLE [Monitor];";
        var sqlTcp = "TRUNCATE TABLE [MonitorTcp];";
        var sqlHttp = "TRUNCATE TABLE [MonitorHttp];";
        var sqlAgentTasks = "TRUNCATE TABLE [MonitorAgentTasks];";
        var sqlAlerts = "TRUNCATE TABLE [MonitorAlert];";
        var sqlHistory = "TRUNCATE TABLE [MonitorHistory];";
        var sqlNotification = "TRUNCATE TABLE [MonitorNotification];";
        var sqlMonitorGroupItems = "TRUNCATE TABLE [MonitorGroupItems];";
        var sqlMonitorGroup = "TRUNCATE TABLE [MonitorGroup];";
        await db.ExecuteAsync(sqlMonitor, commandType: CommandType.Text);
        await db.ExecuteAsync(sqlTcp, commandType: CommandType.Text);
        await db.ExecuteAsync(sqlHttp, commandType: CommandType.Text);
        await db.ExecuteAsync(sqlAgentTasks, commandType: CommandType.Text);
        await db.ExecuteAsync(sqlAlerts, commandType: CommandType.Text);
        await db.ExecuteAsync(sqlHistory, commandType: CommandType.Text);
        await db.ExecuteAsync(sqlNotification, commandType: CommandType.Text);
        await db.ExecuteAsync(sqlMonitorGroupItems, commandType: CommandType.Text);
        await db.ExecuteAsync(sqlMonitorGroup, commandType: CommandType.Text);
    }

    public async Task<IEnumerable<Monitor?>> GetMonitorList(MonitorEnvironment environment)
    {
        await using var db = new SqlConnection(_connstring);
        string sql =
            @"SELECT M.Id, M.Name, HTTP.UrlToCheck, CAST(IP AS VARCHAR(255)) + ':' + CAST(Port AS VARCHAR(10)) AS MonitorTcp, MonitorTypeId, HeartBeatInterval, Retries, Status, DaysToExpireCert, Paused, MonitorRegion, MonitorEnvironment, Tag, HTTP.CheckCertExpiry FROM [Monitor] M
                LEFT JOIN MonitorHttp HTTP on HTTP.MonitorId = M.Id
                LEFT JOIN MonitorTcp TCP ON TCP.MonitorId = M.Id
                WHERE MonitorEnvironment = @environment";
        return await db.QueryAsync<Monitor>(sql, new { environment }, commandType: CommandType.Text);
    }

    public async Task<IEnumerable<Monitor>?> GetMonitorListByMonitorGroupIds(List<int> groupMonitorIds,
        MonitorEnvironment environment)
    {
        await using var db = new SqlConnection(_connstring);
        string sql =
            @$"SELECT Id, Name, MonitorTypeId, HeartBeatInterval, Retries, Status, DaysToExpireCert, Paused, MonitorRegion, MonitorEnvironment, Tag FROM [Monitor] M
            INNER JOIN MonitorGroupItems MGI ON MGI.MonitorId = M.Id WHERE MGI.MonitorGroupId IN @groupMonitorIds AND M.MonitorEnvironment = @environment";
        return await db.QueryAsync<Monitor>(sql, new { groupMonitorIds, environment }, commandType: CommandType.Text);
    }

    public async Task UpdateMonitorHttp(MonitorHttp monitorHttp)
    {
        await using var db = new SqlConnection(_connstring);

        string sqlMonitor = @"UPDATE [dbo].[Monitor]
                    SET [Name] = @Name
                    ,[HeartBeatInterval] = @HeartBeatInterval
                    ,[Retries] = @Retries
                    ,[Status] = @Status
                    ,[DaysToExpireCert] = @DaysToExpireCert
                    ,[Paused] = @Paused
                    ,[MonitorRegion] = @MonitorRegion
                    ,[MonitorEnvironment] = @MonitorEnvironment
                    WHERE Id = @MonitorId";
        await db.ExecuteAsync(sqlMonitor,
            new
            {
                monitorHttp.MonitorId,
                monitorHttp.Name,
                monitorHttp.HeartBeatInterval,
                monitorHttp.Retries,
                monitorHttp.Status,
                monitorHttp.DaysToExpireCert,
                monitorHttp.Paused,
                monitorHttp.MonitorRegion,
                monitorHttp.MonitorEnvironment
            }, commandType: CommandType.Text);

        string sqlMonitorHttp =
            @"UPDATE [MonitorHttp] SET CheckCertExpiry = @CheckCertExpiry, IgnoreTlsSsl = @IgnoreTlsSsl,
            MaxRedirects = @MaxRedirects, UrlToCheck = @UrlToCheck, Timeout = @Timeout, MonitorHttpMethod = @MonitorHttpMethod,
            Body = @Body, HeadersJson = @HeadersJson WHERE MonitorId = @monitorId";

        await db.ExecuteAsync(sqlMonitorHttp,
            new
            {
                monitorHttp.MonitorId,
                monitorHttp.CheckCertExpiry,
                monitorHttp.IgnoreTlsSsl,
                monitorHttp.MaxRedirects,
                monitorHttp.MonitorHttpMethod,
                monitorHttp.Body,
                monitorHttp.HeadersJson,
                monitorHttp.UrlToCheck,
                monitorHttp.Timeout
            }, commandType: CommandType.Text);
    }

    public async Task DeleteMonitor(int id)
    {
        await using var db = new SqlConnection(_connstring);
        string sql = @"DELETE FROM [Monitor] WHERE Id=@id";
        await db.ExecuteAsync(sql, new { id }, commandType: CommandType.Text);

        string sqlTasks = @"DELETE FROM [MonitorAgentTasks] WHERE MonitorId=@id";
        await db.ExecuteAsync(sqlTasks, new { id }, commandType: CommandType.Text);

        string sqlAlerts = @"DELETE FROM [MonitorAlert] WHERE MonitorId=@id";
        await db.ExecuteAsync(sqlAlerts, new { id }, commandType: CommandType.Text);

        string sqlHttp = @"DELETE FROM [MonitorHttp] WHERE MonitorId=@id";
        await db.ExecuteAsync(sqlHttp, new { id }, commandType: CommandType.Text);

        string sqlTcp = @"DELETE FROM [MonitorTcp] WHERE MonitorId=@id";
        await db.ExecuteAsync(sqlTcp, new { id }, commandType: CommandType.Text);

        string sqlNotification = @"DELETE FROM [MonitorNotification] WHERE MonitorId=@id";
        await db.ExecuteAsync(sqlNotification, new { id }, commandType: CommandType.Text);

        string sqlMonitorGroupItems = @"DELETE FROM [MonitorGroupItems] WHERE MonitorId=@id";
        await db.ExecuteAsync(sqlMonitorGroupItems, new { id }, commandType: CommandType.Text);

        // Enqueue the deletion of history as a background job
        BackgroundJob.Enqueue(() => DeleteMonitorHistory(id));
    }

    public void DeleteMonitorHistory(int id)
    {
        // This method will be executed in the background
        using var db = new SqlConnection(_connstring);
        string sqlHistory = @"DELETE FROM [MonitorHistory] WHERE MonitorId=@id";
        db.Execute(sqlHistory, new { id }, commandType: CommandType.Text, commandTimeout: 1800);
    }

    public async Task<int> CreateMonitorTcp(MonitorTcp monitorTcp)
    {
        await using var db = new SqlConnection(_connstring);
        string sqlMonitor =
            @"INSERT INTO [Monitor] (Name, MonitorTypeId, HeartBeatInterval, Retries, Status, DaysToExpireCert, Paused, MonitorRegion, MonitorEnvironment, Tag)
            VALUES (@Name, @MonitorTypeId, @HeartBeatInterval, @Retries, @Status, @DaysToExpireCert, @Paused, @MonitorRegion, @MonitorEnvironment, @Tag); SELECT CAST(SCOPE_IDENTITY() as int)";
        var id = await db.ExecuteScalarAsync<int>(sqlMonitor,
            new
            {
                monitorTcp.Name,
                monitorTcp.MonitorTypeId,
                monitorTcp.HeartBeatInterval,
                monitorTcp.Retries,
                monitorTcp.Status,
                monitorTcp.DaysToExpireCert,
                monitorTcp.Paused,
                monitorTcp.MonitorRegion,
                monitorTcp.MonitorEnvironment,
                monitorTcp.Tag
            }, commandType: CommandType.Text);

        string sqlMonitorTcp =
            @"INSERT INTO [MonitorTcp] (MonitorId, Port, IP, Timeout, LastStatus) VALUES (@MonitorId, @Port, @IP, @Timeout, @LastStatus)";
        await db.ExecuteAsync(sqlMonitorTcp,
            new
            {
                MonitorId = id,
                monitorTcp.Port,
                monitorTcp.IP,
                monitorTcp.Timeout,
                monitorTcp.LastStatus
            }, commandType: CommandType.Text);
        return id;
    }

    public async Task UpdateMonitorTcp(MonitorTcp monitorTcp)
    {
        await using var db = new SqlConnection(_connstring);

        string sqlMonitor = @"UPDATE [dbo].[Monitor]
                    SET [Name] = @Name
                    ,[HeartBeatInterval] = @HeartBeatInterval
                    ,[Retries] = @Retries
                    ,[Status] = @Status
                    ,[DaysToExpireCert] = @DaysToExpireCert
                    ,[Paused] = @Paused
                    ,[MonitorRegion] = @MonitorRegion
                    ,[MonitorEnvironment] = @MonitorEnvironment
                    WHERE Id = @MonitorId";
        await db.ExecuteAsync(sqlMonitor,
            new
            {
                monitorTcp.MonitorId,
                monitorTcp.Name,
                monitorTcp.HeartBeatInterval,
                monitorTcp.Retries,
                monitorTcp.Status,
                monitorTcp.DaysToExpireCert,
                monitorTcp.Paused,
                monitorTcp.MonitorRegion,
                monitorTcp.MonitorEnvironment
            }, commandType: CommandType.Text);

        string sqlMonitorTcp =
            @"UPDATE [MonitorTcp] SET MonitorId = @MonitorId, Port = @Port, IP = @IP, Timeout = @Timeout, LastStatus = @LastStatus WHERE MonitorId = @MonitorId";
        await db.ExecuteAsync(sqlMonitorTcp,
            new
            {
                monitorTcp.MonitorId,
                monitorTcp.Port,
                monitorTcp.IP,
                monitorTcp.Timeout,
                monitorTcp.LastStatus
            }, commandType: CommandType.Text);
    }

    public async Task<IEnumerable<MonitorTcp>> GetTcpMonitorByIds(List<int> ids)
    {
        await using var db = new SqlConnection(_connstring);

        string sql =
            $@"SELECT MonitorId, Port, IP, Timeout, LastStatus  FROM [MonitorTcp] WHERE MonitorId IN @ids";

        return await db.QueryAsync<MonitorTcp>(sql, new { ids }, commandType: CommandType.Text);
    }

    public async Task<IEnumerable<Monitor>> GetMonitorListByIds(List<int> ids)
    {
        await using var db = new SqlConnection(_connstring);
        string sql =
            $@"SELECT Id, Name, MonitorTypeId, HeartBeatInterval, Retries, Status, DaysToExpireCert, Paused, MonitorRegion, MonitorEnvironment, Tag FROM [Monitor] WHERE Id IN @ids";
        return await db.QueryAsync<Monitor>(sql, new { ids }, commandType: CommandType.Text);
    }

    public async Task<IEnumerable<Monitor>> GetMonitorListbyTag(string Tag)
    {
        await using var db = new SqlConnection(_connstring);

        string sql =
            $@"SELECT Id, Name, MonitorTypeId, HeartBeatInterval, Retries, Status, DaysToExpireCert, Paused, MonitorRegion, MonitorEnvironment, Tag FROM [Monitor] WHERE Tag = @Tag";
        return await db.QueryAsync<Monitor>(sql, new { Tag }, commandType: CommandType.Text);
    }

    public async Task<IEnumerable<string?>> GetMonitorTagList()
    {
        await using var db = new SqlConnection(_connstring);
        string sql = "SELECT DISTINCT(Tag) FROM [Monitor] WHERE Tag IS NOT NULL";
        return await db.QueryAsync<string>(sql, commandType: CommandType.Text);
    }

    public async Task<Monitor> GetMonitorById(int id)
    {
        await using var db = new SqlConnection(_connstring);

        string sql =
            $@"SELECT Id, Name, MonitorTypeId, HeartBeatInterval, Retries, Status, DaysToExpireCert, Paused, MonitorRegion, MonitorEnvironment, Tag FROM [Monitor] WHERE Id=@id";
        return await db.QueryFirstOrDefaultAsync<Monitor>(sql, new { id }, commandType: CommandType.Text);
    }

    public async Task<MonitorHttp> GetHttpMonitorByMonitorId(int monitorId)
    {
        await using var db = new SqlConnection(_connstring);

        string sql =
            $@"SELECT a.Id, a.Name, a.MonitorTypeId, a.HeartBeatInterval, a.Retries, a.Status, a.DaysToExpireCert, a.Paused, a.MonitorRegion, a.MonitorEnvironment, a.Tag,
               b.MonitorId, b.CheckCertExpiry, b.IgnoreTlsSsl, b.MaxRedirects, b.UrlToCheck, b.Timeout, b.MonitorHttpMethod, b.Body, b.HeadersJson
                FROM [Monitor] a inner join
                [MonitorHttp] b on a.Id = b.MonitorId
             WHERE MonitorId = @monitorId";
        return await db.QueryFirstOrDefaultAsync<MonitorHttp>(sql, new { monitorId }, commandType: CommandType.Text);
    }

    public async Task<MonitorTcp> GetTcpMonitorByMonitorId(int monitorId)
    {
        await using var db = new SqlConnection(_connstring);

        string sql =
            $@"SELECT a.Id, a.Name, a.MonitorTypeId, a.HeartBeatInterval, a.Retries, a.Status, a.DaysToExpireCert, a.Paused, a.MonitorRegion, a.MonitorEnvironment, a.Tag,
               b.MonitorId, b.Port, b.IP, b.Timeout, b.LastStatus
                FROM [Monitor] a inner join
                [MonitorTcp] b on a.Id = b.MonitorId
            WHERE MonitorId = @monitorId";
        return await db.QueryFirstOrDefaultAsync<MonitorTcp>(sql, new { monitorId }, commandType: CommandType.Text);
    }

    public async Task UpdateMonitorStatus(int id, bool status, int daysToExpireCert)
    {
        await using var db = new SqlConnection(_connstring);
        string sql = @"UPDATE [Monitor] SET Status=@status, DaysToExpireCert=@daysToExpireCert WHERE Id=@id";
        await db.ExecuteAsync(sql, new { id, status, daysToExpireCert }, commandType: CommandType.Text);
    }

    public async Task PauseMonitor(int id, bool paused)
    {
        await using var db = new SqlConnection(_connstring);
        string sql = @"UPDATE [Monitor] SET paused=@paused WHERE Id=@id";
        await db.ExecuteAsync(sql, new { id, paused }, commandType: CommandType.Text);

        if (paused)
        {
            var sqlRemoveTasks = @"DELETE FROM [MonitorAgentTasks] WHERE MonitorId=@id";
            await db.ExecuteAsync(sqlRemoveTasks, new { id }, commandType: CommandType.Text);
        }
    }

    public async Task<int> CreateMonitorHttp(MonitorHttp monitorHttp)
    {
        await using var db = new SqlConnection(_connstring);
        string sqlMonitor =
            @"INSERT INTO [Monitor] (Name, MonitorTypeId, HeartBeatInterval, Retries, Status, DaysToExpireCert, Paused, MonitorRegion, MonitorEnvironment) VALUES (@Name, @MonitorTypeId, @HeartBeatInterval, @Retries, @Status, @DaysToExpireCert, @Paused, @MonitorRegion, @MonitorEnvironment); SELECT CAST(SCOPE_IDENTITY() as int)";
        var id = await db.ExecuteScalarAsync<int>(sqlMonitor,
            new
            {
                monitorHttp.Name,
                monitorHttp.MonitorTypeId,
                monitorHttp.HeartBeatInterval,
                monitorHttp.Retries,
                monitorHttp.Status,
                monitorHttp.DaysToExpireCert,
                monitorHttp.Paused,
                monitorHttp.MonitorRegion,
                monitorHttp.MonitorEnvironment
            }, commandType: CommandType.Text);

        string sqlMonitorHttp =
            @"INSERT INTO [MonitorHttp] (MonitorId, CheckCertExpiry, IgnoreTlsSsl, MaxRedirects, UrlToCheck, Timeout, MonitorHttpMethod, Body, HeadersJson)
        VALUES (@MonitorId, @CheckCertExpiry, @IgnoreTlsSsl, @MaxRedirects, @UrlToCheck, @Timeout, @MonitorHttpMethod, @Body, @HeadersJson)";
        await db.ExecuteAsync(sqlMonitorHttp,
            new
            {
                MonitorId = id,
                monitorHttp.CheckCertExpiry,
                monitorHttp.IgnoreTlsSsl,
                monitorHttp.MaxRedirects,
                monitorHttp.MonitorHttpMethod,
                monitorHttp.Body,
                monitorHttp.HeadersJson,
                monitorHttp.UrlToCheck,
                monitorHttp.Timeout
            }, commandType: CommandType.Text);
        return id;
    }

    public async Task<IEnumerable<MonitorHttp>> GetHttpMonitorByIds(List<int> ids)
    {
        await using var db = new SqlConnection(_connstring);

        string sql =
            $@"SELECT MonitorId, CheckCertExpiry, IgnoreTlsSsl, MaxRedirects, UrlToCheck, Timeout, MonitorHttpMethod, Body, HeadersJson FROM [MonitorHttp] WHERE MonitorId IN @ids";

        return await db.QueryAsync<MonitorHttp>(sql, new { ids }, commandType: CommandType.Text);
    }

    public async Task<IEnumerable<MonitorFailureCount>> GetMonitorFailureCount(int days)
    {
        await using var db = new SqlConnection(_connstring);
        string sql =
            @$"SELECT MonitorId, COUNT(Status) AS FailureCount
            FROM MonitorAlert
            WHERE Status = 'false' AND TimeStamp >= DATEADD(DAY, -@days, GETDATE())
            GROUP BY MonitorId;";

        return await db.QueryAsync<MonitorFailureCount>(sql, new { days }, commandType: CommandType.Text);
    }
}