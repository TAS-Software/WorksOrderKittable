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
    
    public partial class WOKittable_WOOpenLines
    {
        public long WOKittable_ID { get; set; }
        public string WorksOrderNumber { get; set; }
        public string RespCode { get; set; }
        public string CompPartNumber { get; set; }
        public string CompPG { get; set; }
        public decimal PlannedIssueQty { get; set; }
        public decimal ActualIssueQty { get; set; }
        public bool IsFullyIssued { get; set; }
        public decimal ActualKitNeed { get; set; }
        public string CommercialNotes { get; set; }
        public string BatchNotes { get; set; }
        public System.DateTime PlannedIssueDate { get; set; }
        public decimal Stock { get; set; }
    }
}
