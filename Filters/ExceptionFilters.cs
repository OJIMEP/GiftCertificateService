using Microsoft.AspNetCore.Mvc.Filters;

namespace GiftCertificateService.Filters
{
    public class ConnectionResetExceptionFilter : IAsyncExceptionFilter
    {

        private readonly ILogger<ConnectionResetExceptionFilter> _logger;

        public ConnectionResetExceptionFilter(ILogger<ConnectionResetExceptionFilter> logger)
        {
            _logger = logger;
        }

        public Task OnExceptionAsync(ExceptionContext context)
        {

            if (context.Exception is Microsoft.AspNetCore.Connections.ConnectionResetException)
            {
                context.ExceptionHandled = true;

                _logger.LogInformation("Соединение сброшено");
            }
            return Task.CompletedTask;
        }
    }
}
