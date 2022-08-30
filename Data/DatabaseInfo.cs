namespace GiftCertificateService.Data
{
    public class DatabaseInfo : DatabaseConnectionParameter, ICloneable
    {
        public string ConnectionWithoutCredentials { get; set; }
        public bool AvailableToUse { get; set; }
        public DateTimeOffset LastFreeProcCacheCommand { get; set; }
        public DateTimeOffset LastCheckAvailability { get; set; }
        public DateTimeOffset LastCheckAggregations { get; set; }
        public DateTimeOffset LastCheckPerformance { get; set; }
        public int ActualPriority { get; set; }
        public bool ExistsInFile { get; set; }
        public bool CustomAggregationsAvailable { get; set; }
        public int CustomAggregationsFailCount { get; set; }
        public int TimeCriteriaFailCount { get; set; }
        public DatabaseType DatabaseType { get; set; }  

        public DatabaseInfo(DatabaseConnectionParameter connectionParameter)
        {
            Connection = connectionParameter.Connection;
            ConnectionWithoutCredentials = RemoveCredentialsFromConnectionString(Connection);
            Priority = connectionParameter.Priority;
            ActualPriority = connectionParameter.Priority;

            switch (connectionParameter.Type)
            {
                case "main":
                    DatabaseType = DatabaseType.Main;
                    break;
                case "replica_full":
                    DatabaseType = DatabaseType.ReplicaFull;
                    break;
                case "replica_tables":
                    DatabaseType = DatabaseType.ReplicaTables;
                    break;
            }
        }

        public object Clone()
        {
            var result = new DatabaseInfo(this)
            {
                AvailableToUse = AvailableToUse,
                LastFreeProcCacheCommand = LastFreeProcCacheCommand,
                LastCheckAvailability = LastCheckAvailability,
                LastCheckAggregations = LastCheckAggregations,
                LastCheckPerformance = LastCheckPerformance,
                ActualPriority = ActualPriority,
                ExistsInFile = ExistsInFile,
                CustomAggregationsAvailable = CustomAggregationsAvailable,
                CustomAggregationsFailCount = CustomAggregationsFailCount,
                TimeCriteriaFailCount = TimeCriteriaFailCount,
                DatabaseType = DatabaseType
            };

            return result;
        }

        private static string RemoveCredentialsFromConnectionString(string connectionString)
        {
            return string.Join(";",
                connectionString.Split(";")
                    .Where(item => !item.Contains("Uid") && !item.Contains("User") && !item.Contains("Pwd") && !item.Contains("Password") && item.Length > 0));
        }
    }
}
