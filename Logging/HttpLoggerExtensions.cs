﻿using Microsoft.Extensions.Logging;

namespace GiftCertificateService.Logging
{
    public static class HttpLoggerExtensions
    {
        public static ILoggerFactory AddHttp(this ILoggerFactory factory, string host, int port, int portHttp, string env)
        {
            factory.AddProvider(new HttpLoggerProvider(host, port, portHttp, env));
            return factory;
        }
    }
}
