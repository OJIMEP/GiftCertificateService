using GiftCertificateService.Data;
using GiftCertificateService.Exceptions;
using GiftCertificateService.Logging;
using GiftCertificateService.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Diagnostics;

namespace GiftCertificateService.Services
{
    public class CertService : ICertService
    {
        private readonly ILoadBalancing _loadBalancing;
        private readonly Stopwatch _watch;
        private readonly ElasticLogElementDto logElement;
        private List<string> barcodesList;

        public CertService(ILoadBalancing loadBalancing)
        {
            _loadBalancing = loadBalancing;
            _watch = new();
            barcodesList = new();
            logElement = new();
        }

        public async Task<List<ResponseCertGet>> GetCertsInfoByListAsync(List<string> barcodes)
        {
            barcodesList = barcodes;
            var result = new List<ResponseCertGet>();

            SqlConnection sqlConnection = await GetSqlConnectionAsync();

            _watch.StartMeasure();
            try
            {
                SqlCommand sqlCommand = GetSqlCommandCertInfo(sqlConnection);

                result = await GetCertsInfoResult(sqlCommand);

                logElement.SetResponse(result);
                logElement.SetStatistics(sqlConnection.RetrieveStatistics());
            }
            catch (Exception ex)
            {
                logElement.SetError(ex.Message);
            }
            logElement.SetExecutionFact(_watch.EndMeasure());

            _ = sqlConnection.CloseAsync();

            return result;
        }

        private async Task<List<ResponseCertGet>> GetCertsInfoResult(SqlCommand sqlCommand)
        {
            List<ResponseCertGet> result = new();

            SqlDataReader dataReader = await sqlCommand.ExecuteReaderAsync();

            while (await dataReader.ReadAsync())
            {
                var dbBarcode = dataReader.GetString(0);

                result.Add(new ResponseCertGet
                {
                    Barcode = barcodesList.Find(x => x.ToUpper() == dbBarcode) ?? dbBarcode,
                    Sum = dataReader.GetDecimal(1)
                });
            }

            _ = dataReader.CloseAsync();

            return result;
        }

        private async Task<SqlConnection> GetSqlConnectionAsync()
        {
            bool loadBalancingError = false;
            string loadBalancingErrorDescription = string.Empty;

            DbConnection dbConnection = new();

            _watch.StartMeasure();
            try
            {
                dbConnection = await _loadBalancing.GetDatabaseConnectionAsync();
            }
            catch (Exception ex)
            {
                loadBalancingError = true;
                loadBalancingErrorDescription = ex.Message;
            }
            
            if (!loadBalancingError && dbConnection.Connection == null)
            {
                loadBalancingError = true;
                loadBalancingErrorDescription = "Не найдено доступное соединение к БД";
            }

            logElement.SetLoadBalancingExecution(_watch.EndMeasure());
            logElement.SetDatabaseConnection(dbConnection.ConnectionWithoutCredentials);

            if (loadBalancingError)
            {
                logElement.SetError(loadBalancingErrorDescription);
                throw new DBConnectionNotFoundException(loadBalancingErrorDescription);
            }

            SqlConnection result = dbConnection.Connection!;

            result.StatisticsEnabled = true;

            return result;
        }

        private SqlCommand GetSqlCommandCertInfo(SqlConnection connection)
        {
            List<string> barcodesUpperCase = barcodesList.Select(x => x.ToUpper()).Distinct().ToList();

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

        public ElasticLogElementDto GetLog()
        {
            return logElement;
        }
    }
}
