using System.Collections;
using System.Text.Json;

namespace GiftCertificateService.Logging
{
    public class ElasticLogElementDTO
    {
        public string? ResponseContent { get; set; }
        public long TimeSQLExecution { get; set; }
        public long TimeSQLExecutionFact { get; set; }
        public LogStatus Status { get; set; }
        public string ErrorDescription { get; set; }
        public string? DatabaseConnection { get; set; }
        public long LoadBalancingExecution { get; set; }
        public Dictionary<string, string?> AdditionalData { get; set; }

        public ElasticLogElementDTO()
        {
            AdditionalData = new();
            ErrorDescription = string.Empty;
            Status = LogStatus.Ok;
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
    }
}
