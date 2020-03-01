using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorksOrderKittable
{
    class WOKittingGroupedExport
    {
        public string WONumber { get; set; }
        public string ResponsibilityCode { get; set; }
        public decimal WOTotalPlannedQty { get; set; }
        public decimal TotalPlannedQty { get; set; }
        public decimal TotalActualQty { get; set; }
        public decimal totalActualKitNeed { get; set; }
        public decimal TotalKittable { get; set; }
        public string CommercialNotes { get; set; }
        public string BatchNotes { get; set; }
        public decimal PartsNotFound { get; set; }
        public decimal OverKitted { get; set; }
    }
}
