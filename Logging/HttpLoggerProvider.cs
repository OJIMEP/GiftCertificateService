namespace GiftCertificateService.Logging
{
    public class HttpLoggerProvider : ILoggerProvider
    {
        private readonly string _host;
        private readonly int _port;
        private readonly int _portHttp;
        private readonly string _env;
        public HttpLoggerProvider(string host, int port, int portHttp, string env)
        {
            _host = host;
            _port = port;
            _portHttp = portHttp;
            _env = env;
        }
        public ILogger CreateLogger(string categoryName)
        {
            return new HttpLogger(_host, _port, _portHttp, _env);
        }

        public void Dispose()
        {
        }
    }
}
