namespace GiftCertificateService.Data
{
    public class DatabaseConnectionParameter
    {
        public string Connection { get; set; } = "";
        public int Priority { get; set; }
        public string Type { get; set; } = ""; //main, replica_full, replica_tables 
    }
}
