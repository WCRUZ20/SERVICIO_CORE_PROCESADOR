using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.SAP
{
    public class SapItemsTable
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public int Transaction { get; set; }
        public string TransactionType {  get; set; }
        public string SapDocEntry { get; set; }
        public string SapDocNum { get; set; }
        public string SapDocStatus { get; set; }
        public string Json { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string Comment { get; set; }
        public int Status { get; set; }
        public decimal Stock {  get; set; }
    }
}
