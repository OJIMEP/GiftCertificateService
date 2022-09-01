using GiftCertificateService.Logging;
using GiftCertificateService.Services;
using Microsoft.Data.SqlClient;
using System.Diagnostics;

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

            var connectionParameters = _configuration.GetSection("OneSDatabases")
                .Get<List<DatabaseConnectionParameter>>()
                .Select(x => new DatabaseInfo(x));

            var timeMs = DateTime.Now.Millisecond % 100;

            List<string> failedConnections = new();

            bool firstAvailable = false;

            var resultString = "";

            SqlConnection? conn = null;

            while (true)
            {
                int percentCounter = 0;
                foreach (var connParameter in connectionParameters)
                {
                    if (firstAvailable && failedConnections.Contains(connParameter.Connection))
                        continue;

                    Stopwatch watch = new();
                    percentCounter += connParameter.Priority;
                    if (timeMs <= percentCounter && connParameter.Priority != 0 || firstAvailable)
                    {
                        try
                        {
                            watch.StartMeasure();

                            conn = await GetConnectionByDatabaseInfo(connParameter);

                            watch.EndMeasure();

                            resultString = connParameter.Connection;
                            
                            result.Connection = conn;
                            result.DatabaseType = connParameter.DatabaseType;
                            result.UseAggregations = connParameter.CustomAggregationsAvailable;
                            result.ConnectionWithoutCredentials = connParameter.ConnectionWithoutCredentials;
                            break;
                        }
                        catch (Exception ex)
                        {
                            var logElement = new ElasticLogElement(LogStatus.Error)
                            {
                                ErrorDescription = ex.Message,
                                LoadBalancingExecution = watch.EndMeasure(),
                                DatabaseConnection = connParameter.ConnectionWithoutCredentials
                            };

                            _logger.LogMessageGen(logElement.ToString());

                            if (conn != null && conn.State != System.Data.ConnectionState.Closed)
                            {
                                _ = conn.CloseAsync();
                            }

                            failedConnections.Add(connParameter.Connection);
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
            var queryStringCheck = databaseInfo.DatabaseType switch
            {
                DatabaseType.Main => Queries.DatabaseBalancingMain,
                DatabaseType.ReplicaFull => Queries.DatabaseBalancingReplicaFull,
                DatabaseType.ReplicaTables => Queries.DatabaseBalancingReplicaTables,
                _ => ""
            };

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
