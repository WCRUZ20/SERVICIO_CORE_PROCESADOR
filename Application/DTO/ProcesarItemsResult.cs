using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTO
{
    public class ProcesarItemsResult
    {
        public bool IsSuccess { get; set; }
        public int ItemsProcessed { get; set; }
        public int ItemsSent { get; set; }
        public int ItemsFailed { get; set; }
        public List<string>? Errors { get; set; }
        public string? Message { get; set; }
        public string? ErrorMessage { get; set; }
    }

}
