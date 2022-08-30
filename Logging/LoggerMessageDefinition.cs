namespace GiftCertificateService.Logging
{
    public static class LoggerMessageDefinition
    {
        private static readonly Action<ILogger, string, Exception?> LogMessageDefinition =
            LoggerMessage.Define<string>(LogLevel.Information, 0, "{Message}");

        private static readonly Action<ILogger, string, Exception?> LogErrorMessageDefinition =
            LoggerMessage.Define<string>(LogLevel.Error, 0, "{Message}");

        public static void LogMessage(this ILogger logger, string message)
        {
            LogMessageDefinition(logger, message, null);
        }

        public static void LogErrorMessage(this ILogger logger, string message, Exception? exeption)
        {
            LogErrorMessageDefinition(logger, message, exeption);
        }
    }

    public static partial class LoggerMessageDefinitionGen
    {
        [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "{Message}", SkipEnabledCheck = true)]
        public static partial void LogMessageGen(this ILogger logger, string message);
    }
}
