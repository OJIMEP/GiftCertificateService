using System.Text.Json.Serialization;

namespace GiftCertificateService.Models
{
    public class ResponseError
    {
        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }
}
