using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorksOrderKittable
{
    class kittableLine
    {
        public string WorksOrderNumber { get; set; }
        public string PartNumber { get; set; }
        public string RespCode { get; set; }
        public decimal PlannedIssueQty { get; set; }
        public decimal ActualIssueQty { get; set; }
        public decimal ActualKitNeed { get; set; }
        public DateTime PlannedIssueDate { get; set; }
        public decimal Kittable { get; set; }
        public decimal PNF { get; set; }
        public decimal OverKitted { get; set; }
        public string CommercialNotes { get; set; }
        public string BatchNotes { get; set; }
    }
}
