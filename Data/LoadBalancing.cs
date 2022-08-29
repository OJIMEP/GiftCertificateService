using GiftCertificateService.Logging;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Text.Json;

namespace GiftCertificateService.Data
{
    public interface ILoadBalancing
    {
        Task<DbConnection> GetDatabaseConnectionAsync();
    }

    public class LoadBalancing : ILoadBalancing
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<LoadBalancing> _logger;

        public LoadBalancing(IConfiguration configuration, ILogger<LoadBalancing> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<DbConnection> GetDatabaseConnectionAsync()
        {
            var result = new DbConnection();

            var connectionParameters = _configuration.GetSection("OneSDatabases").Get<List<DatabaseConnectionParameter>>().Select(x => new DatabaseInfo(x));

            var timeMS = DateTime.Now.Millisecond % 100;

            List<string> failedConnections = new();

            bool firstAvailable = false;

            var resultString = "";

            SqlConnection? conn = null;

            while (true)
            {
                int percentCounter = 0;
                foreach (var connParametr in connectionParameters)
                {
                    if (firstAvailable && failedConnections.Contains(connParametr.Connection))
                        continue;

                    Stopwatch watch = new();
                    percentCounter += connParametr.Priority;
                    if (timeMS <= percentCounter && connParametr.Priority != 0 || firstAvailable)
                    {
                        try
                        {
                            watch.Start();

                            conn = await GetConnectionByDatabaseInfo(connParametr);

                            watch.Stop();

                            resultString = connParametr.Connection;
                            
                            result.Connection = conn;
                            result.DatabaseType = connParametr.DatabaseType;
                            result.UseAggregations = connParametr.CustomAggregationsAvailable;
                            result.ConnectionWithoutCredentials = connParametr.ConnectionWithoutCredentials;
                            break;
                        }
                        catch (Exception ex)
                        {
                            if (watch.IsRunning)
                            {
                                watch.Stop();
                            }

                            var logElement = new ElasticLogElement(LogStatus.Error)
                            {
                                ErrorDescription = ex.Message,
                                LoadBalancingExecution = watch.ElapsedMilliseconds,
                                DatabaseConnection = connParametr.ConnectionWithoutCredentials
                            };

                            _logger.LogMessageGen(JsonSerializer.Serialize(logElement));

                            if (conn != null && conn.State != System.Data.ConnectionState.Closed)
                            {
                                _ = conn.CloseAsync();
                            }

                            failedConnections.Add(connParametr.Connection);
                        }
                    }
                }

                if (resultString.Length > 0 || firstAvailable)
                    break;
                else
                    firstAvailable = true;
            }

            return result;
        }

        private static async Task<SqlConnection?> GetConnectionByDatabaseInfo(DatabaseInfo databaseInfo)
        {
            var queryStringCheck = "";
            if (databaseInfo.DatabaseType == DatabaseType.Main)
                queryStringCheck = Queries.DatebaseBalancingMain;

            if (databaseInfo.DatabaseType == DatabaseType.ReplicaFull)
                queryStringCheck = Queries.DatebaseBalancingReplicaFull;

            if (databaseInfo.DatabaseType == DatabaseType.ReplicaTables)
                queryStringCheck = Queries.DatebaseBalancingReplicaTables;

            //sql connection object
            SqlConnection connection = new(databaseInfo.Connection);
            await connection.OpenAsync();

            SqlCommand cmd = new(queryStringCheck, connection)
            {
                CommandTimeout = 1
            };

            SqlDataReader dr = await cmd.ExecuteReaderAsync();

            _ = dr.CloseAsync();

            return connection;
        }
    }
}
