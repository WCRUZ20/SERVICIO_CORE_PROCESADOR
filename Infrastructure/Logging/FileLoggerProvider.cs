using Domain.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text;

namespace Infrastructure.Logging
{
    public class FileLoggerProvider : ILoggerProvider
    {
        private readonly string _logDirectory;
        private readonly ConcurrentDictionary<string, FileLogger> _loggers = new();
        private readonly ConcurrentDictionary<string, StreamWriter> _fileWriters = new();
        private readonly object _lockObject = new object();
        private readonly int _retentionDays;
        private readonly LogLevel _minLogLevel;


        public FileLoggerProvider(string logDirectory, IOptions<WorkerSettings> workerSettings)
        {
            _logDirectory = logDirectory ?? throw new ArgumentNullException(nameof(logDirectory));

            // Obtener RetentionDays desde configuración, con valor por defecto de 7
            _retentionDays = workerSettings.Value?.RetentionDaysConfig?.RetentionDays ?? 7;

            // Obtener LogLevel desde configuración, con valor por defecto de Information
            var logLevelString = workerSettings.Value?.FileLoggerConfig?.LogLevel ?? "Information";
            if (!Enum.TryParse<LogLevel>(logLevelString, ignoreCase: true, out _minLogLevel))
            {
                _minLogLevel = LogLevel.Information;
            }

            // Crear directorio de logs si no existe
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, name => 
                new FileLogger(name, this, _logDirectory, _minLogLevel));
        }

        internal void WriteToFile(string logDirectory, string logEntry)
        {
            var fileName = $"{DateTime.Now:yyyy-MM-dd}.txt";
            var filePath = Path.Combine(logDirectory, fileName);

            lock (_lockObject)
            {
                try
                {
                    // Obtener o crear el StreamWriter para el archivo del día
                    var writer = _fileWriters.GetOrAdd(fileName, _ =>
                    {
                        // Cerrar writers de días anteriores si existen
                        CleanupOldWriters(fileName);
                        CleanupOldLogFiles(); // borra logs de dias anteriores
                        var fileStream = new FileStream(
                            filePath,
                            FileMode.Append,
                            FileAccess.Write,
                            FileShare.ReadWrite);
                        
                        return new StreamWriter(fileStream, Encoding.UTF8)
                        {
                            AutoFlush = true
                        };
                    });

                    writer.WriteLine(logEntry);
                }
                catch
                {
                    // Si hay error al escribir, intentar recrear el writer
                    _fileWriters.TryRemove(fileName, out _);
                    throw;
                }
            }
        }

        private void CleanupOldWriters(string currentFileName)
        {
            // Cerrar y remover writers de archivos que no son del día actual
            var filesToRemove = _fileWriters.Keys
                .Where(key => key != currentFileName)
                .ToList();

            foreach (var fileKey in filesToRemove)
            {
                if (_fileWriters.TryRemove(fileKey, out var oldWriter))
                {
                    try
                    {
                        oldWriter.Dispose();
                    }
                    catch
                    {
                        // Ignorar errores al cerrar
                    }
                }
            }
        }

        public void Dispose()
        {
            foreach (var writer in _fileWriters.Values)
            {
                try
                {
                    writer.Dispose();
                }
                catch
                {
                    // Ignorar errores al cerrar
                }
            }
            _fileWriters.Clear();
            _loggers.Clear();
        }

        private void CleanupOldLogFiles()
        {
            try
            {
                var cutoffDate = DateTime.Now.Date.AddDays(-_retentionDays);

                var logFiles = Directory.GetFiles(_logDirectory, "*.txt");

                foreach (var file in logFiles)
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);

                    if (DateTime.TryParseExact(
                            fileName,
                            "yyyy-MM-dd",
                            null,
                            System.Globalization.DateTimeStyles.None,
                            out var fileDate))
                    {
                        if (fileDate < cutoffDate)
                        {
                            File.Delete(file);
                        }
                    }
                }
            }
            catch
            {
                // Logging no debe romper la app
            }
        }

    }
}