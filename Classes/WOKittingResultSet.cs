using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorksOrderKittable
{
    class WOKittingResultSet
    {
        public string PartNumber { get; set; }
        public string Description { get; set; }
        public string WONumber { get; set; }
        public Nullable<System.DateTime> WODueDate { get; set; }
        public string ProductGroup { get; set; }
        public string PartMethod { get; set; }
        public string Responsibility { get; set; }
        public string CommercialNotes { get; set; }
        public string BatchNotes { get; set; }
        public Nullable<decimal> Demand { get; set; }
        public Nullable<decimal> DemandForThisDate { get; set; }
        public Nullable<decimal> NetShortage { get; set; }
        public Nullable<decimal> StockAfterThisDate { get; set; }
        public string WORaisedBy { get; set; }
        public string ParentAssembly { get; set; }
        public string ParentAssemblyDescription { get; set; }
        public string Store1 { get; set; }
        public string Store2 { get; set; }
        public string Store3 { get; set; }
        public string OtherGood { get; set; }
        public string BadLocations { get; set; }
        public string binLocation { get; set; }
        public Nullable<decimal> GoodStock { get; set; }
        public Nullable<decimal> BadStock { get; set; }

    }
}
