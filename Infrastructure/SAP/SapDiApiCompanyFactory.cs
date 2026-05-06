using Application.Interfaces.SAP;
using Domain.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SAPbobsCOM;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Infrastructure.SAP
{
    public sealed class SapDiApiCompanyFactory : ISapDiApiCompanyFactory
    {
        private readonly SapDiApiSettings _settings;
        private readonly ILogger<SapDiApiCompanyFactory> _logger;

        public SapDiApiCompanyFactory(IOptions<SapDiApiSettings> settings, ILogger<SapDiApiCompanyFactory> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public Task<ISapCompanySession> CreateSessionAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var cfg = _settings.Company ?? new DiApiCompanyConfig();

            if (string.IsNullOrWhiteSpace(cfg.Server) ||
                string.IsNullOrWhiteSpace(cfg.LicenseServer) ||
                string.IsNullOrWhiteSpace(cfg.CompanyDb) ||
                string.IsNullOrWhiteSpace(cfg.UserName) ||
                string.IsNullOrWhiteSpace(cfg.Password))
            {
                throw new InvalidOperationException("Configuración DI API incompleta: verifique SapDiApiSettings:Company.");
            }

            var company = new Company
            {
                Server = cfg.Server.Trim(),
                LicenseServer = cfg.LicenseServer.Trim(),
                CompanyDB = cfg.CompanyDb.Trim(),
                UserName = cfg.UserName.Trim(),
                Password = cfg.Password,
            };

            if (!string.IsNullOrWhiteSpace(cfg.DbUserName))
                company.DbUserName = cfg.DbUserName.Trim();
            if (!string.IsNullOrWhiteSpace(cfg.DbPassword))
                company.DbPassword = cfg.DbPassword;

            if (!string.IsNullOrWhiteSpace(cfg.DbServerType))
                company.DbServerType = ParseDbServerType(cfg.DbServerType);

            if (!string.IsNullOrWhiteSpace(cfg.Language))
                company.language = ParseLanguage(cfg.Language);

            if (cfg.UseSld)
            {
                if (string.IsNullOrWhiteSpace(cfg.SldServer))
                    throw new InvalidOperationException("UseSld=true pero SldServer está vacío en SapDiApiSettings:Company.");

                company.UseTrusted = false;
                company.SLDServer = cfg.SldServer.Trim();
            }

            var rc = company.Connect();
            if (rc != 0)
            {
                company.GetLastError(out var code, out var message);
                SafeRelease(company);
                throw new InvalidOperationException($"DI API Connect falló. Code={code}. Message={message}");
            }

            _logger.LogInformation("Conexión DI API establecida a CompanyDB={CompanyDb}", company.CompanyDB);
            return Task.FromResult<ISapCompanySession>(new SapCompanySession(company, _logger));
        }

        private static BoDataServerTypes ParseDbServerType(string value)
        {
            var normalized = value.Trim().ToUpperInvariant();
            return normalized switch
            {
                "HANA" or "HANADB" => BoDataServerTypes.dst_HANADB,
                "MSSQL2019" => BoDataServerTypes.dst_MSSQL2019,
                "MSSQL2017" => BoDataServerTypes.dst_MSSQL2017,
                "MSSQL2016" => BoDataServerTypes.dst_MSSQL2016,
                "MSSQL2014" => BoDataServerTypes.dst_MSSQL2014,
                "MSSQL2012" => BoDataServerTypes.dst_MSSQL2012,
                "MSSQL2008" => BoDataServerTypes.dst_MSSQL2008,
                "MSSQL" => BoDataServerTypes.dst_MSSQL,
                _ => BoDataServerTypes.dst_HANADB
            };
        }

        private static BoSuppLangs ParseLanguage(string value)
        {
            var normalized = value.Trim().ToLowerInvariant();
            return normalized switch
            {
                "spanish" or "es" or "español" => BoSuppLangs.ln_Spanish,
                "english" or "en" => BoSuppLangs.ln_English,
                _ => BoSuppLangs.ln_Spanish
            };
        }

        private static void SafeRelease(object? comObject)
        {
            if (comObject == null) return;
            try
            {
                if (Marshal.IsComObject(comObject))
                    Marshal.FinalReleaseComObject(comObject);
            }
            catch
            {
                // Ignorar: el proceso debe continuar.
            }
        }

        private sealed class SapCompanySession : ISapCompanySession
        {
            private readonly ILogger _logger;
            private bool _disposed;

            public SapCompanySession(Company company, ILogger logger)
            {
                Company = company;
                _logger = logger;
            }

            public Company Company { get; }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;

                try
                {
                    if (Company.Connected)
                        Company.Disconnect();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error al desconectar Company DI API.");
                }
                finally
                {
                    SafeRelease(Company);
                }
            }

            public ValueTask DisposeAsync()
            {
                Dispose();
                return ValueTask.CompletedTask;
            }
        }
    }
}

