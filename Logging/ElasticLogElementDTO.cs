using System.Collections;
using System.Text.Json;

namespace GiftCertificateService.Logging
{
    public class ElasticLogElementDto
    {
        public string? ResponseContent { get; set; }
        public long TimeSqlExecution { get; set; }
        public long TimeSqlExecutionFact { get; set; }
        public LogStatus Status { get; set; }
        public string ErrorDescription { get; set; }
        public string? DatabaseConnection { get; set; }
        public long LoadBalancingExecution { get; set; }
        public Dictionary<string, string?> AdditionalData { get; set; }

        public ElasticLogElementDto()
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
            TimeSqlExecution = (long)(stats["ExecutionTime"] ?? 0);
            AdditionalData.Add("stats", JsonSerializer.Serialize(stats));
        }

        public void SetExecutionFact(long elapsedMilliseconds)
        {
            TimeSqlExecutionFact = elapsedMilliseconds;
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
