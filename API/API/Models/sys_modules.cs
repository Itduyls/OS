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
    
    public partial class sys_modules
    {
        public int module_id { get; set; }
        public Nullable<int> parent_id { get; set; }
        public string module_name { get; set; }
        public string image { get; set; }
        public string icon { get; set; }
        public string is_link { get; set; }
        public string is_filepath { get; set; }
        public string is_stand { get; set; }
        public string is_target { get; set; }
        public int is_order { get; set; }
        public bool status { get; set; }
        public bool is_admin { get; set; }
        public Nullable<int> organization_id { get; set; }
        public string is_size { get; set; }
        public string created_by { get; set; }
        public Nullable<System.DateTime> created_date { get; set; }
        public string created_ip { get; set; }
        public string created_token_id { get; set; }
        public string modified_by { get; set; }
        public Nullable<System.DateTime> modified_date { get; set; }
        public string modified_token_id { get; set; }
        public string modified_ip { get; set; }
    
        public virtual sys_organization sys_organization { get; set; }
    }
}
