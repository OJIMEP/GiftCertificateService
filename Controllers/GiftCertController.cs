using FluentValidation;
using FluentValidation.AspNetCore;
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
        private readonly ILoadBalancing _loadBalacing;
        private readonly IValidator<string> _validatorSingle;
        private readonly IValidator<List<string>> _validatorMultiple;

        public GiftCertController(ILogger<GiftCertController> logger,
                                  ILoadBalancing loadBalacing,
                                  IValidator<List<string>> validatorMultiple,
                                  IValidator<string> validatorSingle)
        {
            _logger = logger;
            _loadBalacing = loadBalacing;
            _validatorMultiple = validatorMultiple;
            _validatorSingle = validatorSingle;
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
            var validationResult = _validatorSingle.Validate(barcode);
            
            if (!validationResult.IsValid)
            {
                return BadRequest(new ResponseError { Error = validationResult.ToString() });
            }

            List<ResponseCertGet>? result;

            var barcodesList = new List<string>
            {
                barcode
            };

            try
            {
                result = await GetCertInfoByBarcodes(barcodesList);
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
                return BadRequest(new ResponseError { Error = "Certs aren't valid"});
            }

            return Ok(result.First());
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

            var validationResult = _validatorMultiple.Validate(barcode);

            if (!validationResult.IsValid)
            {
                return BadRequest(new ResponseError { Error = validationResult.ToString() });
            }

            List<ResponseCertGet>? result;

            try
            {
                result = await GetCertInfoByBarcodes(barcode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(500, new ResponseError{ Error = "Internal server error" });
            }

            if (result is null)
            {
                return StatusCode(500, new { error = "Internal server error" });
            }

            if (result.Count == 0)
            {
                return BadRequest(new ResponseError { Error = "Certs aren't valid" });
            }

            return Ok(result.ToArray());
        }

        private async Task<List<ResponseCertGet>?> GetCertInfoByBarcodes(List<string> barcodes)
        {
            
            var barcodesUpperCase = barcodes.Select(x => x.ToUpperInvariant()).Distinct().ToList();

            var logElementLoadBal = new ElasticLogElement
            {
                Path = HttpContext.Request.Path.ToString()+"("+ HttpContext.Request.Method+")",
                Host = HttpContext.Request.Host.ToString(),
                RequestContent = JsonSerializer.Serialize(barcodes),
                Id = Guid.NewGuid().ToString(),
                AuthenticatedUser = User.Identity.Name
            };

            SqlConnection conn;
            Stopwatch watch = new();
            watch.Start();
            try
            {
                //connString = await _loadBalacing.GetDatabaseConnectionAsync();
                var dbConnection = await _loadBalacing.GetDatabaseConnectionAsync();
                conn = dbConnection.Connection;
               
            }
            catch (Exception ex)
            {
                //connString = "";
                logElementLoadBal.TimeSQLExecution = 0;
                logElementLoadBal.ErrorDescription = ex.Message;
                logElementLoadBal.Status = "Error";
                var logstringElement1 = JsonSerializer.Serialize(logElementLoadBal);
                _logger.LogInformation(logstringElement1);
                return null;
            }
            watch.Stop();


            if (conn == null)
            {
                logElementLoadBal.TimeSQLExecution = 0;
                logElementLoadBal.ErrorDescription = "Не найдено доступное соединение к БД";
                logElementLoadBal.Status = "Error";
                logElementLoadBal.LoadBalancingExecution = watch.ElapsedMilliseconds;
                var logstringElement1 = JsonSerializer.Serialize(logElementLoadBal);
                _logger.LogInformation(logstringElement1);

                return null;
            }

            

            var logElement = new ElasticLogElement
            {
                Path = HttpContext.Request.Path.ToString() + "(" + HttpContext.Request.Method + ")",
                Host = HttpContext.Request.Host.ToString(),
                RequestContent = JsonSerializer.Serialize(barcodes),
                Id = Guid.NewGuid().ToString(),
                DatabaseConnection = LoadBalancing.RemoveCredentialsFromConnectionString(conn.ConnectionString),
                AuthenticatedUser = User.Identity.Name,
                LoadBalancingExecution = watch.ElapsedMilliseconds
            };

            logElement.AdditionalData.Add("Referer", Request.Headers["Referer"].ToString());
            logElement.AdditionalData.Add("User-Agent", Request.Headers["User-Agent"].ToString());
            logElement.AdditionalData.Add("RemoteIpAddress", Request.HttpContext.Connection.RemoteIpAddress.ToString());

            var result = new List<ResponseCertGet>();

            long sqlCommandExecutionTime = 0;
            watch.Start();
            try
            {

                conn.StatisticsEnabled = true;

                string query = Queries.CertInfo;

                var DateMove = DateTime.Now.AddMonths(24000);

                //define the SqlCommand object
                SqlCommand cmd = new(query, conn);

                List<string> barcodeParameters = new();
                foreach (var barcode in barcodesUpperCase)
                {
                    var parameterString = string.Format("@PickupPointAll{0}", barcodesUpperCase.IndexOf(barcode));
                    barcodeParameters.Add(parameterString);
                    cmd.Parameters.Add(parameterString, SqlDbType.NVarChar, 12);
                    cmd.Parameters[parameterString].Value = barcode;
                }

                query = query.Replace("@Barcode", String.Join(",", barcodeParameters));

                cmd.CommandTimeout = 5;

                cmd.CommandText = query;

                //execute the SQLCommand
                SqlDataReader dr = cmd.ExecuteReader();

                //check if there are records
                if (dr.HasRows)
                {
                    while (dr.Read())
                    {
                        var dbBarcode = dr.GetString(0);

                        var incomeBarcode = barcodes.Find(x => x.ToUpperInvariant() == dbBarcode); //in case of low characters

                        var resultItem = new ResponseCertGet
                        {
                            Barcode = incomeBarcode ?? dbBarcode,
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
                logElement.AdditionalData.Add("stats", JsonSerializer.Serialize(stats));
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
            return result;
        }
    }
}
