using Microsoft.Data.SqlClient;

namespace GiftCertificateService.Data
{
    public class DbConnection
    {
        public SqlConnection? Connection { get; set; }
        public DatabaseType DatabaseType { get; set; }
        public bool UseAggregations { get; set; }
        public string ConnectionWithoutCredentials { get; set; } = "";
    }
}
