using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorksOrderKittable
{
    class KittingShortage
    {
        public string WorksOrderNumber { get; set; }
        public string PartNumber { get; set; }
        public string ProductGroup { get; set; }
        public decimal QuantityShort { get; set; }
    }
}
