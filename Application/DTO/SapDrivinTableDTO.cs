using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.SAP
{


    /// <summary>
    /// Representa la tabla de control de documentos enviados a SAP
    /// </summary>
    public class SapDrivinTableDTO
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public int Transaction { get; set; }
        public int SapDocEntry { get; set; }
        public int SapDocNum { get; set; }
        public string SapDocStatus { get; set; }
        public string Json { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string Comment { get; set; }
        public int Status { get; set; }


    }


}
