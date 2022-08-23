using FluentValidation;
//using FluentValidation.AspNetCore;
using GiftCertificateService.Data;
using GiftCertificateService.Logging;
using GiftCertificateService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Diagnostics;
using System.Text.Json;

namespace GiftCertificateService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GiftCertController : ControllerBase
    {
        private readonly ILogger<GiftCertController> _logger;
        private readonly ILoadBalancing _loadBalancing;
        private readonly IValidator<List<string>> _validatorMultiple;

        class DBConnectionNotFoundException : SystemException
        {
            public DBConnectionNotFoundException(string message) : base(message)
            {
            }
        }

        public GiftCertController(ILogger<GiftCertController> logger,
                                  ILoadBalancing loadBalacing,
                                  IValidator<List<string>> validatorMultiple)
        {
            _logger = logger;
            _loadBalancing = loadBalacing;
            _validatorMultiple = validatorMultiple;
        }

        /// <summary>
        /// Returns info about one cert's barcode
        /// </summary>
        /// <param name="barcode">Certificate code. Length 11 symbols, only latin and digits are allowed</param>
        /// <returns>Info about cert</returns>
        /// <remarks>
        /// Sample request:
        ///
        ///     GET /api/GiftCert?barcode=AAO11111111
        ///             
        /// </remarks>
        /// <response code="200">Returns info about valid cert</response>
        /// <response code="400">If barcodes format is invalid or certs are invalid</response>
        /// <response code="401">Not authorized</response>
        /// <response code="500">Any server error</response>
        [Authorize]
        [HttpGet]
        [Produces("application/json")]
        [ProducesResponseType(typeof(ResponseCertGet), 200)]
        [ProducesResponseType(typeof(ResponseError), 400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(typeof(ResponseError), 500)]
        public async Task<IActionResult> GetInfoAsync(string barcode)
        {
            var barcodesList = new List<string>
            {
                barcode
            };

            return await GetInfoByListAsync(barcodesList, true);
        }

        /// <summary>
        /// Returns info about multiple cert's barcodes
        /// </summary>
        /// <param name="barcode">Array of certificate codes. Length 11 symbols, only latin and digits are allowed</param>
        /// <returns>Info about certs. Only valid barcodes presents in the result.</returns>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST /api/GiftCert
        ///     ["AA000000BBB", "AA000001BBB"]
        ///             
        /// </remarks>
        /// <response code="200">Returns info about valid certs</response>
        /// <response code="400">If any of barcodes format is invalid or all certs are invalid</response>
        /// <response code="401">Not authorized</response>
        /// <response code="500">Any server error</response>
        [Authorize]
        [HttpPost]
        [Produces("application/json")]
        [ProducesResponseType(typeof(IEnumerable<ResponseCertGet>), 200)]
        [ProducesResponseType(typeof(ResponseError), 400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(typeof(ResponseError), 500)]
        public async Task<IActionResult> GetInfoMultipleAsync([FromBody]List<string> barcode)
        {
            return await GetInfoByListAsync(barcode);
        }

        private async Task<IActionResult> GetInfoByListAsync(List<string> barcodeList, bool single = false)
        {
            var validationResult = _validatorMultiple.Validate(barcodeList);

            if (!validationResult.IsValid)
            {
                return BadRequest(new ResponseError { Error = validationResult.ToString() });
            }

            List<ResponseCertGet>? result;

            try
            {
                result = await GetInfoFromDatabaseByListAsync(barcodeList);
            }
            catch (DBConnectionNotFoundException)
            {
                return StatusCode(500, new ResponseError { Error = "Available database connection not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(500, new ResponseError { Error = "Internal server error" });
            }

            if (result is null)
            {
                return StatusCode(500, new ResponseError { Error = "Internal server error" });
            }

            if (result.Count == 0)
            {
                return BadRequest(new ResponseError { Error = "Certs aren't valid" });
            }

            if (single)
            {
                return Ok(result.First());
            }
            else
            {
                return Ok(result.ToArray());
            }
        }

        private async Task<List<ResponseCertGet>?> GetInfoFromDatabaseByListAsync(List<string> barcodes)
        {           
            var barcodesUpperCase = barcodes.Select(x => x.ToUpper()).Distinct().ToList();

            var logElement = InitElasticLogElement();
            logElement.RequestContent = JsonSerializer.Serialize(new { request = JsonSerializer.Serialize(barcodes) });

            DbConnection dbConnection = await GetDatabaseConnection(logElement);

            SqlConnection sqlConnection = dbConnection.Connection!;

            var result = new List<ResponseCertGet>();

            long sqlCommandExecutionTime = 0;

            Stopwatch watch = new();
            watch.Start();
            try
            {
                sqlConnection.StatisticsEnabled = true;

                //execute the SQLCommand
                SqlDataReader dataReader = await GetSqlCommandCertInfo(sqlConnection, barcodesUpperCase).ExecuteReaderAsync();

                while (dataReader.Read())
                {
                    var dbBarcode = dataReader.GetString(0);

                    result.Add(new ResponseCertGet
                    {
                        Barcode = barcodes.Find(x => x.ToUpper() == dbBarcode) ?? dbBarcode,
                        Sum = dataReader.GetDecimal(1)
                    });
                }

                var stats = sqlConnection.RetrieveStatistics();
                sqlCommandExecutionTime = (long)stats["ExecutionTime"]!;

                //close data reader
                dataReader.Close();

                logElement.TimeSQLExecution = sqlCommandExecutionTime;
                logElement.ResponseContent = JsonSerializer.Serialize(new { response = JsonSerializer.Serialize(result) });
                logElement.AdditionalData.Add("stats", JsonSerializer.Serialize(stats));
            }
            catch (Exception ex)
            {
                logElement.SetError(ex.Message);
            }
            watch.Stop();
            logElement.TimeSQLExecutionFact = watch.ElapsedMilliseconds;
            sqlConnection.Close();
            
            _logger.LogInformation(JsonSerializer.Serialize(logElement));

            return result;
        }

        private async Task<DbConnection> GetDatabaseConnection(ElasticLogElement logElement)
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
                _logger.LogInformation(JsonSerializer.Serialize(logElement));
                throw new DBConnectionNotFoundException(loadBalancingErrorDescription);
            }

            return dbConnection;
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

        private ElasticLogElement InitElasticLogElement()
        {
            ElasticLogElement result = new(LogStatus.Ok)
            {
                Path = $"{HttpContext.Request.Path}({HttpContext.Request.Method})",
                Host = HttpContext.Request.Host.ToString(),
                Id = Guid.NewGuid().ToString(),
                AuthenticatedUser = User?.Identity?.Name
            };

            result.AdditionalData.Add("Referer", Request.Headers["Referer"].ToString());
            result.AdditionalData.Add("User-Agent", Request.Headers["User-Agent"].ToString());
            result.AdditionalData.Add("RemoteIpAddress", Request?.HttpContext?.Connection?.RemoteIpAddress?.ToString());

            return result;
        }
    }
}
