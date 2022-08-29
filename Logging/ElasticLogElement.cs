﻿using System.Text.Json.Serialization;

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
        public long TimeBtsExecution { get; set; }
        public long TimeLocationExecution { get; set; }
        public long LoadBalancingExecution { get; set; }
        public long GlobalParametersExecution { get; set; }
        public Dictionary<string, string?> AdditionalData { get; set; }
        public string Enviroment { get; set; }
        public string ServiceName { get; set; }

        public ElasticLogElement(LogStatus status)
        {
            Enviroment = EnviromentStatic.Enviroment ?? "Unset";
            AdditionalData = new();
            ServiceName = "CertInfo";
            Status = status;
            ErrorDescription = "";
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

        public static ElasticLogElement InitElasticLogElement(HttpContext httpContext, HttpRequest request)
        {
            ElasticLogElement result = new(LogStatus.Ok)
            {
                Path = $"{httpContext.Request.Path}({httpContext.Request.Method})",
                Host = httpContext.Request.Host.ToString(),
                Id = Guid.NewGuid().ToString(),
                AuthenticatedUser = httpContext.User?.Identity?.Name
            };

            result.AdditionalData.Add("Referer", request.Headers["Referer"].ToString());
            result.AdditionalData.Add("User-Agent", request.Headers["User-Agent"].ToString());
            result.AdditionalData.Add("RemoteIpAddress", request?.HttpContext?.Connection?.RemoteIpAddress?.ToString());

            return result;
        }
    }
}
