//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace API.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class doc_ca_types
    {
        public int doc_type_id { get; set; }
        public string doc_type_name { get; set; }
        public string doc_type_code { get; set; }
        public Nullable<int> nav_type { get; set; }
        public Nullable<int> is_order { get; set; }
        public Nullable<bool> status { get; set; }
        public Nullable<int> organization_id { get; set; }
    }
}
