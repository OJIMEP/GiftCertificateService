using FluentValidation;
using GiftCertificateService.Exceptions;
using GiftCertificateService.Logging;
using GiftCertificateService.Models;
using GiftCertificateService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GiftCertificateService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GiftCertController : ControllerBase
    {
        private readonly ILogger<GiftCertController> _logger;
        private readonly IValidator<List<string>> _validatorMultiple;
        private readonly ICertService _certService;

        public GiftCertController(ILogger<GiftCertController> logger,
                                  IValidator<List<string>> validatorMultiple,
                                  ICertService certService)
        {
            _logger = logger;
            _validatorMultiple = validatorMultiple;
            _certService = certService;
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
            var result = await GetInfoByListAsync(barcode);
            return result;
        }

        private async Task<IActionResult> GetInfoByListAsync(List<string> barcodeList, bool single = false)
        {
            var validationResult = _validatorMultiple.Validate(barcodeList);

            if (!validationResult.IsValid)
            {
                return BadRequest(new ResponseError { Error = validationResult.ToString() });
            }

            List<ResponseCertGet> result;

            try
            {
                result = await _certService.GetCertsInfoByListAsync(barcodeList);
            }
            catch (DBConnectionNotFoundException)
            {
                return StatusCode(500, new ResponseError { Error = "Available database connection not found" });
            }
            catch (Exception ex)
            {
                _logger.LogErrorMessage(ex.Message, ex);
                return StatusCode(500, new ResponseError { Error = "Internal server error" });
            }
            finally
            {
                var logElement = new ElasticLogElement(HttpContext, Request, _certService.GetLog());
                logElement.SetRequest(barcodeList);
                _logger.LogMessageGen(logElement.ToString());
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

    }
}
