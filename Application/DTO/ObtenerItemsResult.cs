using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTO
{
    public class ObtenerItemsResult
    {
        public bool IsSuccess { get; set; }
        public int ItemsFound { get; set; }
        public int ItemsInserted { get; set; }
        public string? Message { get; set; }
    }

}
