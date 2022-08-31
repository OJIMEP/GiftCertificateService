using DateTimeService.Areas.Identity.Models;
using System.Text.Json.Serialization;

namespace GiftCertificateService.Contracts.V1.Responses
{
    public class LoginResponse
    {
        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;
        [JsonPropertyName("expiration")]
        public DateTime Expiration { get; set; }
        [JsonPropertyName("refresh")]
        public string Refresh { get; set; } = string.Empty;
        [JsonPropertyName("expiration_refresh")]
        public DateTime Expiration_refresh { get; set; }

        public LoginResponse()
        {
        }

        public LoginResponse(AuthenticateResponse response)
        {
            Token = response.JwtToken;
            Expiration = response.JwtValidTo;
            Refresh = response.RefreshToken;
            Expiration_refresh = response.RefreshValidTo;
        }
    }
}
