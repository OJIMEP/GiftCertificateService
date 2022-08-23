using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace GiftCertificateService.Logging
{
    public class HttpLogger : ILogger
    {
        private readonly string logsHost;
        private readonly int logsPortUdp;
        private readonly int logsPortHttp;
        readonly UdpClient udpClient;
        readonly HttpClient httpClient;

        public HttpLogger(string host, int port, int portHttp, string _env)
        {
            logsHost = host;
            logsPortUdp = port;
            logsPortHttp = portHttp;
            udpClient = new UdpClient(logsHost, logsPortUdp);
            httpClient = new HttpClient();
            EnviromentStatic.Enviroment = _env;
        }
        public IDisposable BeginScope<TState>(TState state) => default!;

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public async void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (formatter != null)
            {
                var logMessage = new ElasticLogMessage();
                if (!formatter(state, exception).Contains("ResponseContent"))
                {
                    var logElement = new ElasticLogElement(LogStatus.Info)
                    {
                        TimeSQLExecution = 0,
                        ErrorDescription = formatter(state, exception),
                    };

                    if (exception != null)
                    {
                        logElement.SetError(exception.Message);
                        logElement.AdditionalData.Add("StackTrace", exception.StackTrace);
                    }

                    var logstringElement = JsonSerializer.Serialize(logElement);
                    logMessage.Message.Add(logstringElement);
                }
                else
                {
                    logMessage.Message.Add(formatter(state, exception));
                }

                var resultLog = JsonSerializer.Serialize(logMessage);

                byte[] sendBytes = Encoding.UTF8.GetBytes(resultLog);

                try
                {
                    if (sendBytes.Length > 60000)
                    {
                        var result = await httpClient.PostAsync(
                            new Uri($"http://{logsHost}:{logsPortHttp:D}"), 
                            new StringContent(resultLog, Encoding.UTF8, "application/json")
                        );
                    }
                    else
                        await udpClient.SendAsync(sendBytes, sendBytes.Length);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }
    }
}
