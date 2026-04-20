using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Helper
{


    /// <summary>
    /// Representa el resultado de una respuesta Sí/No
    /// </summary>
    public class YesNoQuestion
    {
        /// <summary>
        /// Resultado : 1 = Sí, 0 = No
        /// </summary>
        public int Resultado { get; set; }
        public int Cantidad { get; set; }

        
    }

}
