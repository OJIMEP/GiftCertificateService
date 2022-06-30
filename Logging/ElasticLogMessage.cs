using System.Text.Json.Serialization;

namespace GiftCertificateService.Logging
{
    public class ElasticLogMessage
    {
        [JsonPropertyName("message")]
        public List<string> Message { get; set; }

        public ElasticLogMessage()
        {
            Message = new List<string>();
        }
    }
}
