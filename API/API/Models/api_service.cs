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
    
    public partial class api_service
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public api_service()
        {
            this.api_parameters = new HashSet<api_parameters>();
        }
    
        public int service_id { get; set; }
        public int category_id { get; set; }
        public string service_name { get; set; }
        public string des { get; set; }
        public Nullable<bool> is_app { get; set; }
        public int is_order { get; set; }
        public Nullable<bool> status { get; set; }
        public string created_by { get; set; }
        public Nullable<System.DateTime> created_date { get; set; }
        public string created_ip { get; set; }
        public string created_token_id { get; set; }
        public string data { get; set; }
        public string url_file { get; set; }
        public string proc_name { get; set; }
    
        public virtual api_category api_category { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<api_parameters> api_parameters { get; set; }
    }
}
