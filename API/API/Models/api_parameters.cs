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
    
    public partial class api_parameters
    {
        public int parameters_id { get; set; }
        public int service_id { get; set; }
        public Nullable<int> table_id { get; set; }
        public string parameters_name { get; set; }
        public string des { get; set; }
        public string example_value { get; set; }
        public int is_order { get; set; }
        public Nullable<bool> status { get; set; }
        public string created_by { get; set; }
        public Nullable<System.DateTime> created_date { get; set; }
        public string created_ip { get; set; }
        public string created_token_id { get; set; }
        public string parameters_type { get; set; }
    
        public virtual api_service api_service { get; set; }
    }
}