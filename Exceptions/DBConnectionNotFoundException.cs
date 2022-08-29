namespace GiftCertificateService.Exceptions
{
    class DBConnectionNotFoundException : SystemException
    {
        public DBConnectionNotFoundException(string message) : base(message)
        {
        }
    }
}
