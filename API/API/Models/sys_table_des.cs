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
    
    public partial class sys_table_des
    {
        public int id { get; set; }
        public string db_name { get; set; }
        public string table_name { get; set; }
        public string title { get; set; }
        public string des { get; set; }
        public Nullable<bool> is_pag { get; set; }
        public Nullable<bool> is_formdata { get; set; }
        public string template { get; set; }
        public string config { get; set; }
        public string code_vue { get; set; }
        public string code_api { get; set; }
        public string code_sql { get; set; }
        public string code_flutter_view { get; set; }
        public string code_flutter_controller { get; set; }
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