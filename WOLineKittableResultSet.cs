//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace WorksOrderKittable
{
    using System;
    using System.Collections.Generic;
    
    public partial class WOLineKittableResultSet
    {
        public long WOLineKittable_ID { get; set; }
        public long UID { get; set; }
        public string WONumber { get; set; }
        public string RespCode { get; set; }
        public decimal PlannedIssueQty { get; set; }
        public decimal ActualIssueQty { get; set; }
        public decimal ActualKitNeed { get; set; }
        public System.DateTime PlannedIssueDate { get; set; }
        public decimal Kittable { get; set; }
        public decimal PNF { get; set; }
        public decimal OverKitted { get; set; }
        public string CommercialNotes { get; set; }
        public string BatchNotes { get; set; }
        public System.DateTime DateRun { get; set; }
        public string PartNumber { get; set; }
    }
}
