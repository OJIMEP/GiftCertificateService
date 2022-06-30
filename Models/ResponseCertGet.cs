using System.Text.Json.Serialization;

namespace GiftCertificateService.Models
{
    public class ResponseCertGet
    {
        [JsonPropertyName("barcode")]
        public string? Barcode { get; set; }
        [JsonPropertyName("sum")]
        public decimal Sum { get; set; }
    }
}
