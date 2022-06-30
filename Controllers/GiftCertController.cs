using GiftCertificateService.Data;
using GiftCertificateService.Logging;
using GiftCertificateService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;

namespace GiftCertificateService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GiftCertController : ControllerBase
    {
        private readonly ILogger<GiftCertController> _logger;
        private readonly ILoadBalancing _loadBalacing;

        public GiftCertController(ILogger<GiftCertController> logger, ILoadBalancing loadBalacing)
        {
            _logger = logger;
            _loadBalacing = loadBalacing;
        }

        [Authorize]
        [HttpGet("{barcode}")]
        public async Task<ObjectResult> GetInfoAsync(string barcode)
        {
            var dbConnection = await _loadBalacing.GetDatabaseConnectionAsync();
            var conn = dbConnection.Connection;


            var result = new List<ResponseCertGet>();
            var logElement = new ElasticLogElement
            {
                Path = HttpContext.Request.Path,
                Host = HttpContext.Request.Host.ToString(),
                RequestContent = JsonSerializer.Serialize(barcode),
                Id = Guid.NewGuid().ToString(),
                DatabaseConnection = LoadBalancing.RemoveCredentialsFromConnectionString(conn.ConnectionString),
                AuthenticatedUser = User.Identity.Name
            };

            long sqlCommandExecutionTime = 0;

            try
            {

                conn.StatisticsEnabled = true;

                string query = Queries.CertInfo;


                var DateMove = DateTime.Now.AddMonths(24000);

                //define the SqlCommand object
                SqlCommand cmd = new(query, conn);

                cmd.Parameters.Add("@Barcode", SqlDbType.NVarChar);
                cmd.Parameters["@Barcode"].Value = barcode;

                cmd.CommandTimeout = 5;

                cmd.CommandText = query;

                //execute the SQLCommand
                SqlDataReader dr = cmd.ExecuteReader();

                //check if there are records
                if (dr.HasRows)
                {
                    while (dr.Read())
                    {
                        var resultItem = new ResponseCertGet
                        {
                            Barcode = dr.GetString(0),
                            Sum = dr.GetDecimal(1)
                        };

                        result.Add(resultItem);
                    }
                }

                var stats = conn.RetrieveStatistics();
                sqlCommandExecutionTime = (long)stats["ExecutionTime"];

                //close data reader
                dr.Close();

                logElement.TimeSQLExecution = sqlCommandExecutionTime;
                logElement.ResponseContent = JsonSerializer.Serialize(result);
                logElement.Status = "Ok";
            }
            catch (Exception ex)
            {
                logElement.TimeSQLExecution = sqlCommandExecutionTime;
                logElement.ErrorDescription = ex.Message;
                logElement.Status = "Error";
            }

            conn.Close();

            var logstringElement = JsonSerializer.Serialize(logElement);

            _logger.LogInformation(logstringElement);

            return Ok(result.ToArray());
        }

        [Authorize]
        [HttpPost]
        public async Task<ObjectResult> GetInfoMultipleAsync([FromBody]List<string> barcode)
        {
            var dbConnection = await _loadBalacing.GetDatabaseConnectionAsync();
            var conn = dbConnection.Connection;


            var result = new List<ResponseCertGet>();
            var logElement = new ElasticLogElement
            {
                Path = HttpContext.Request.Path,
                Host = HttpContext.Request.Host.ToString(),
                RequestContent = JsonSerializer.Serialize(barcode),
                Id = Guid.NewGuid().ToString(),
                DatabaseConnection = LoadBalancing.RemoveCredentialsFromConnectionString(conn.ConnectionString),
                AuthenticatedUser = User.Identity.Name
            };

            long sqlCommandExecutionTime = 0;

            try
            {

                conn.StatisticsEnabled = true;

                string query = Queries.CertInfo;


                var DateMove = DateTime.Now.AddMonths(24000);

                //define the SqlCommand object
                SqlCommand cmd = new(query, conn);

                cmd.Parameters.Add("@Barcode", SqlDbType.NVarChar);
                cmd.Parameters["@Barcode"].Value = barcode;

                cmd.CommandTimeout = 5;

                cmd.CommandText = query;

                //execute the SQLCommand
                SqlDataReader dr = cmd.ExecuteReader();

                //check if there are records
                if (dr.HasRows)
                {
                    while (dr.Read())
                    {
                        var resultItem = new ResponseCertGet
                        {
                            Barcode = dr.GetString(0),
                            Sum = dr.GetDecimal(1)
                        };

                        result.Add(resultItem);
                    }
                }

                var stats = conn.RetrieveStatistics();
                sqlCommandExecutionTime = (long)stats["ExecutionTime"];

                //close data reader
                dr.Close();

                logElement.TimeSQLExecution = sqlCommandExecutionTime;
                logElement.ResponseContent = JsonSerializer.Serialize(result);
                logElement.Status = "Ok";
            }
            catch (Exception ex)
            {
                logElement.TimeSQLExecution = sqlCommandExecutionTime;
                logElement.ErrorDescription = ex.Message;
                logElement.Status = "Error";
            }

            conn.Close();

            var logstringElement = JsonSerializer.Serialize(logElement);

            _logger.LogInformation(logstringElement);

            return Ok(result.ToArray());
        }
    }
}
