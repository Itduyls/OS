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
    
    public partial class file_share
    {
        public int id { get; set; }
        public Nullable<int> file_id { get; set; }
        public Nullable<int> folder_id { get; set; }
        public string user_id { get; set; }
        public bool status { get; set; }
        public Nullable<bool> is_view { get; set; }
        public Nullable<System.DateTime> date_view { get; set; }
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
