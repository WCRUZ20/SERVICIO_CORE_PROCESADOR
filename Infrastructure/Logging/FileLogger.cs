using Microsoft.Extensions.Logging;
using System.Text;

namespace Infrastructure.Logging
{
    public class FileLogger : ILogger
    {
        private readonly string _name;
        private readonly FileLoggerProvider _provider;
        private readonly string _logDirectory;
        private readonly LogLevel _minLogLevel;

        public FileLogger(string name, FileLoggerProvider provider, string logDirectory, LogLevel minLogLevel)
        {
            _name = name;
            _provider = provider;
            _logDirectory = logDirectory;
            _minLogLevel = minLogLevel;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= _minLogLevel;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var message = formatter(state, exception);
            if (string.IsNullOrEmpty(message) && exception == null)
            {
                return;
            }

            var logEntry = FormatLogEntry(logLevel, _name, eventId, message, exception);
            
            try
            {
                _provider.WriteToFile(_logDirectory, logEntry);
            }
            catch
            {
                // Ignorar errores de escritura para no interrumpir la aplicación
            }
        }

        private static string FormatLogEntry(
            LogLevel logLevel,
            string category,
            EventId eventId,
            string message,
            Exception? exception)
        {
            var logLevelString = logLevel switch
            {
                LogLevel.Trace => "TRACE",
                LogLevel.Debug => "DEBUG",
                LogLevel.Information => "INFO",
                LogLevel.Warning => "WARN",
                LogLevel.Error => "ERROR",
                LogLevel.Critical => "CRITICAL",
                _ => logLevel.ToString().ToUpper()
            };

            var timestamp = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz");
            var eventIdString = eventId.Id != 0 ? $" [{eventId}]" : string.Empty;
            var exceptionString = exception != null 
                ? $"{Environment.NewLine}{exception}" 
                : string.Empty;

            return $"[{timestamp}] [{logLevelString}]{eventIdString} [{category}] {message}{exceptionString}";
        }
    }
}