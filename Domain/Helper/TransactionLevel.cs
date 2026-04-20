using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Helper
{
    public enum TransactionLevel
    {
        //public enum TransactionLevel
        //{


            /// <summary>
            /// Logs that are used for interactive investigation during development.  These logs should primarily contain
            /// Codigo para la Entrega en SAP
            /// </summary>
            Delivery = 1,

            /// <summary>
            /// Codigo para la Transferencia en SAP
            /// </summary>
            StockTransfer = 2,

            
        //}
    }
}
