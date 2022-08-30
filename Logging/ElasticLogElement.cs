using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GiftCertificateService.Logging
{
    public class ElasticLogElement
    {
        public string? Id { get; set; }
        public string? Path { get; set; }
        public string? Host { get; set; }
        public string? ResponseContent { get; set; }
        public string? RequestContent { get; set; }
        public long TimeSQLExecution { get; set; }
        public long TimeSQLExecutionFact { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))] 
        public LogStatus Status { get; set; }
        public string ErrorDescription { get; set; }
        public long TimeFullExecution { get; set; }
        public string? DatabaseConnection { get; set; }
        public string? AuthenticatedUser { get; set; }
        public long LoadBalancingExecution { get; set; }
        public Dictionary<string, string?> AdditionalData { get; set; }
        public string Enviroment { get; set; }
        public string ServiceName { get; set; }

        public ElasticLogElement(LogStatus status)
        {
            Enviroment = EnviromentStatic.Enviroment ?? "Unset";
            AdditionalData = new();
            ServiceName = "CertInfo";
            ErrorDescription = "";
            Status = status;
        }
        public ElasticLogElement(HttpContext httpContext, HttpRequest request) : this(LogStatus.Ok)
        {
            Path = $"{httpContext.Request.Path}({httpContext.Request.Method})";
            Host = httpContext.Request.Host.ToString();
            Id = Guid.NewGuid().ToString();
            AuthenticatedUser = httpContext.User?.Identity?.Name;

            AdditionalData.Add("Referer", request.Headers["Referer"].ToString());
            AdditionalData.Add("User-Agent", request.Headers["User-Agent"].ToString());
            AdditionalData.Add("RemoteIpAddress", request?.HttpContext?.Connection?.RemoteIpAddress?.ToString());
        }

        public ElasticLogElement(HttpContext httpContext, HttpRequest request, ElasticLogElementDTO dto) : this(httpContext, request)
        {
            Status = dto.Status; 
            ErrorDescription = dto.ErrorDescription;
            ResponseContent = dto.ResponseContent;
            TimeSQLExecutionFact = dto.TimeSQLExecutionFact;
            LoadBalancingExecution = dto.LoadBalancingExecution;
            DatabaseConnection = dto.DatabaseConnection;

            foreach (var item in dto.AdditionalData)
            {
                AdditionalData.Add(item.Key, item.Value);
            }
        }

        public void SetError(string errorDescription)
        {
            Status = LogStatus.Error;
            if (ErrorDescription == "")
            {
                ErrorDescription = errorDescription;            
            }
            else
            {
                ErrorDescription += $"; {errorDescription}";
            }
        }

        public void SetResponse<T>(T response)
        {
            ResponseContent = JsonSerializer.Serialize(new { response = JsonSerializer.Serialize(response) });
        }

        public void SetRequest<T>(T request)
        {
            RequestContent = JsonSerializer.Serialize(new { request = JsonSerializer.Serialize(request) });
        }

        public void SetStatistics(IDictionary stats)
        {
            TimeSQLExecution = (long)(stats["ExecutionTime"] ?? 0);
            AdditionalData.Add("stats", JsonSerializer.Serialize(stats));
        }

        public void SetExecutionFact(long elapsedMilliseconds)
        {
            TimeSQLExecutionFact = elapsedMilliseconds;
        }

        public void SetLoadBalancingExecution(long elapsedMilliseconds)
        {
            LoadBalancingExecution = elapsedMilliseconds;
        }

        public void SetDatabaseConnection(string connectionString)
        {
            DatabaseConnection = connectionString;
        }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}
