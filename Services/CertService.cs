using GiftCertificateService.Controllers;
using GiftCertificateService.Data;
using GiftCertificateService.Exceptions;
using GiftCertificateService.Logging;
using GiftCertificateService.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Diagnostics;
using System.Text.Json;

namespace GiftCertificateService.Services
{
    public class CertService : ICertService
    {
        private readonly ILogger<GiftCertController> _logger;
        private readonly ILoadBalancing _loadBalancing;

        public CertService(ILogger<GiftCertController> logger, ILoadBalancing loadBalancing)
        {
            _logger = logger;
            _loadBalancing = loadBalancing;
        }

        public async Task<List<ResponseCertGet>> GetCertsInfoByListAsync(List<string> barcodes, ElasticLogElement logElement)
        {
            logElement.RequestContent = JsonSerializer.Serialize(new { request = JsonSerializer.Serialize(barcodes) });

            SqlConnection sqlConnection = await GetDatabaseConnection(logElement);

            var result = new List<ResponseCertGet>();

            Stopwatch watch = new();
            watch.Start();
            try
            {
                //execute the SQLCommand
                SqlDataReader dataReader = await GetSqlCommandCertInfo(sqlConnection,
                    barcodes.Select(x => x.ToUpper()).Distinct().ToList()).ExecuteReaderAsync();

                while (await dataReader.ReadAsync())
                {
                    var dbBarcode = dataReader.GetString(0);

                    result.Add(new ResponseCertGet
                    {
                        Barcode = barcodes.Find(x => x.ToUpper() == dbBarcode) ?? dbBarcode,
                        Sum = dataReader.GetDecimal(1)
                    });
                }

                var stats = sqlConnection.RetrieveStatistics();
                var sqlCommandExecutionTime = stats["ExecutionTime"] ?? 0;

                _ = dataReader.CloseAsync();

                logElement.TimeSQLExecution = (long)sqlCommandExecutionTime;
                logElement.ResponseContent = JsonSerializer.Serialize(new { response = JsonSerializer.Serialize(result) });
                logElement.AdditionalData.Add("stats", JsonSerializer.Serialize(stats));
            }
            catch (Exception ex)
            {
                logElement.SetError(ex.Message);
            }
            watch.Stop();
            logElement.TimeSQLExecutionFact = watch.ElapsedMilliseconds;
            _ = sqlConnection.CloseAsync();

            //_logger.LogInformation(JsonSerializer.Serialize(logElement));
            _logger.LogMessageGen(JsonSerializer.Serialize(logElement));

            return result;
        }

        private async Task<SqlConnection> GetDatabaseConnection(ElasticLogElement logElement)
        {
            bool loadBalancingError = false;
            string loadBalancingErrorDescription = string.Empty;

            DbConnection dbConnection = new();

            Stopwatch watch = new();
            watch.Start();
            try
            {
                dbConnection = await _loadBalancing.GetDatabaseConnectionAsync();
            }
            catch (Exception ex)
            {
                loadBalancingError = true;
                loadBalancingErrorDescription = ex.Message;
            }
            watch.Stop();

            if (!loadBalancingError && dbConnection.Connection == null)
            {
                loadBalancingError = true;
                loadBalancingErrorDescription = "Не найдено доступное соединение к БД";
            }

            logElement.LoadBalancingExecution = watch.ElapsedMilliseconds;
            logElement.DatabaseConnection = dbConnection.ConnectionWithoutCredentials;

            if (loadBalancingError)
            {
                logElement.SetError(loadBalancingErrorDescription);
                //_logger.LogInformation(JsonSerializer.Serialize(logElement));
                _logger.LogMessageGen(JsonSerializer.Serialize(logElement));
                throw new DBConnectionNotFoundException(loadBalancingErrorDescription);
            }

            SqlConnection result = dbConnection.Connection!;

            result.StatisticsEnabled = true;

            return result;
        }

        private static SqlCommand GetSqlCommandCertInfo(SqlConnection connection, List<string> barcodesUpperCase)
        {
            //define the SqlCommand object
            SqlCommand command = new()
            {
                Connection = connection,
                CommandTimeout = 5
            };

            List<string> barcodeParameters = new();
            for (int i = 0; i < barcodesUpperCase.Count; i++)
            {
                var parameterString = $"@Barcode{i}";
                barcodeParameters.Add(parameterString);
                command.Parameters.Add(parameterString, SqlDbType.NVarChar, 12);
                command.Parameters[parameterString].Value = barcodesUpperCase[i];
            }

            command.CommandText = Queries.CertInfo.Replace("@Barcode", string.Join(",", barcodeParameters));

            return command;
        }

    }
}
