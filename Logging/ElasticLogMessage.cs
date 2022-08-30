using System.Text.Json.Serialization;
using System.Text.Json;

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

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}
