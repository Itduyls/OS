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
    
    public partial class sys_tables
    {
        public int col_id { get; set; }
        public string db_name { get; set; }
        public string table_name { get; set; }
        public string name { get; set; }
        public string title { get; set; }
        public string des { get; set; }
        public string c_type { get; set; }
        public Nullable<int> c_length { get; set; }
        public Nullable<bool> is_null { get; set; }
        public bool required { get; set; }
        public Nullable<bool> is_key { get; set; }
        public Nullable<bool> is_value { get; set; }
        public bool show { get; set; }
        public bool show_form { get; set; }
        public Nullable<bool> is_identity { get; set; }
        public Nullable<bool> search { get; set; }
        public string input { get; set; }
        public string re_table { get; set; }
        public string re_col { get; set; }
        public string css { get; set; }
        public Nullable<int> is_order { get; set; }
        public string created_by { get; set; }
        public Nullable<System.DateTime> created_date { get; set; }
        public string created_ip { get; set; }
        public string created_token_id { get; set; }
        public string modified_by { get; set; }
        public Nullable<System.DateTime> modified_date { get; set; }
        public string modified_token_id { get; set; }
        public string modified_ip { get; set; }
    }
}
