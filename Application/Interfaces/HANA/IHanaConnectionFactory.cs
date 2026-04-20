// Application/Interfaces/HANA/IHanaConnectionFactory.cs
using System.Data.Odbc;
using System.Threading;
using System.Threading.Tasks;
using SAPbobsCOM;
using SAPbouiCOM;
using System.Data.SqlClient;

namespace Application.Interfaces.HANA
{
    /// <summary>
    /// Factory para crear y gestionar conexiones a HANA
    /// </summary>
    public interface IHanaConnectionFactory
    {
        /// <summary>
        /// Crea una nueva conexión a HANA
        /// </summary>
        Task<OdbcConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Valida que la conexión a HANA esté disponible
        /// </summary>
        Task<bool> ValidateConnectionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene el connection string configurado
        /// </summary>
        string GetConnectionString();
    }
}