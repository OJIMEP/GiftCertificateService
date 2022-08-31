using System.Text.Json.Serialization;

namespace GiftCertificateService.Contracts.V1.Responses
{
    public class ErrorResponse
    {
        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }
}
