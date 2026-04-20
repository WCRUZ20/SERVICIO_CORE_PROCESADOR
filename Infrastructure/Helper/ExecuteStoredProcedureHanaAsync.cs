using Domain.Configuration;
using Infrastructure.HANA;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Data.Odbc;

namespace Infrastructure.Helper
{
    public sealed class ExecuteStoredProcedureHanaAsync
    {
        private readonly OdbcSettings _settings;
        private readonly ILogger<HanaRepository> _logger;

        public ExecuteStoredProcedureHanaAsync(

            IOptions<OdbcSettings> settings,
            ILogger<HanaRepository> logger)
        {

            _settings = settings.Value;
            _logger = logger;

        }


        /// <summary>
        /// Ejecuta un stored procedure y retorna los resultados en base a la claase TResult
        /// </summary>
        /// <param name="procedureName">Nombre del stored procedure a ejecutar</param>
        /// <param name="parameters">Diccionario con los parámetros (nombre, valor). Puede ser null si no hay parámetros</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>DataTable con los resultados del stored procedure</returns>
        public async Task<IEnumerable<TResult>> ExecuteStoredProcedureQueryAsync<TResult>(
            OdbcConnection connection,
            string procedureName,
            Dictionary<string, object> parameters,
            CancellationToken cancellationToken = default)
            where TResult : class, new()
        {
            if (string.IsNullOrWhiteSpace(procedureName))
                throw new ArgumentException(
                    "El nombre del stored procedure no puede estar vacío",
                    nameof(procedureName));

            // ==========================
            procedureName = $"{_settings.HANA.Database}." + procedureName;
            // ==========================
            // Construcción del CALL
            // ==========================
            var parameterPlaceholders = parameters != null && parameters.Count > 0
                ? string.Join(", ", Enumerable.Repeat("?", parameters.Count))
                : string.Empty;

            var sql = string.IsNullOrEmpty(parameterPlaceholders)
                ? $"CALL {procedureName}"
                : $"CALL {procedureName}({parameterPlaceholders})";

            using var command = new OdbcCommand(sql, connection);

            // ==========================
            // Parámetros (orden importa en HANA)
            // ==========================
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    //command.Parameters.Add(
                    //    new OdbcParameter
                    //    {
                    //        Value = param.Value ?? DBNull.Value

                    //    });

                    var odbcParam = new OdbcParameter
                    {
                        Value = param.Value ?? DBNull.Value
                    };

                    if (param.Value is DateTime)
                        odbcParam.OdbcType = OdbcType.DateTime;

                    command.Parameters.Add(odbcParam);

                }
            }

            var result = new List<TResult>();

            try
            {
                using var reader = await command
                    .ExecuteReaderAsync(cancellationToken);

                // Cache de propiedades del tipo de retorno
                var properties = typeof(TResult)
                    .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                    .Where(p => p.CanWrite)
                    .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

                while (await reader.ReadAsync(cancellationToken))
                {
                    var entity = new TResult();

                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var columnName = reader.GetName(i);

                        if (!properties.TryGetValue(columnName, out var property))
                            continue;

                        var value = reader.IsDBNull(i)
                            ? null
                            : reader.GetValue(i);

                        property.SetValue(entity, value);
                    }

                    result.Add(entity);
                }

                //_logger.LogInformation(
                //    $"Stored procedure {procedureName} ejecutado exitosamente. Registros retornados: {result.Count}");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"Error al ejecutar stored procedure {procedureName} en HANA: {ex.Message}");
                throw;
            }

            return result;
        }




        public async Task<Boolean> ExecuteStoredProcedureTransactionAsync(
            OdbcConnection connection,
            string procedureName,
            Dictionary<string, object> parameters,
            OdbcTransaction? transaction,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(procedureName))
                throw new ArgumentException(
                    "El nombre del stored procedure no puede estar vacío",
                    nameof(procedureName));

            var _resultFlag = false;

            // ==========================
            // Nombre completo del SP
            // ==========================
            procedureName = $"{_settings.HANA.Database}.{procedureName}";

            // ==========================
            // Construcción del CALL
            // ==========================
            var parameterPlaceholders = parameters != null && parameters.Count > 0
                ? string.Join(", ", Enumerable.Repeat("?", parameters.Count))
                : string.Empty;

            var sql = string.IsNullOrEmpty(parameterPlaceholders)
                ? $"CALL {procedureName}"
                : $"CALL {procedureName}({parameterPlaceholders})";

            using var command = new OdbcCommand(sql, connection)
            {
                Transaction = transaction
            };

            // ==========================
            // Parámetros (orden importa en HANA)
            // ==========================
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    var odbcParam = new OdbcParameter
                    {
                        Value = param.Value ?? DBNull.Value
                    };

                    if (param.Value is DateTime)
                        odbcParam.OdbcType = OdbcType.DateTime;

                    command.Parameters.Add(odbcParam);
                }
            }
            try
            {

                using var reader = await command.ExecuteReaderAsync(cancellationToken);

                if (await reader.ReadAsync(cancellationToken))
                {
                    _resultFlag = Convert.ToBoolean(reader.GetInt32(reader.GetOrdinal("ResultFlag")));

                }


            }

            //catch (Exception ex)
            //{
            //    _logger.LogError(
            //       $"Error al ejecutar stored procedure {procedureName} en HANA: {ex.Message}");

            //}
            catch (OdbcException ex)
            {
                foreach (OdbcError error in ex.Errors)
                {
                    _logger.LogError(
                        "Error HANA en {Procedure}. SQLState={SQLState}, NativeError={NativeError}, Message={Message}",
                        procedureName,
                        error.SQLState,
                        error.NativeError,
                        error.Message);
                }

                throw; // recomendado en arquitectura limpia
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error inesperado al ejecutar stored procedure {Procedure}",
                    procedureName);

                throw;
            }

            return (_resultFlag);

        }



    }
}
