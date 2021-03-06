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
    
    public partial class file_folder
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public file_folder()
        {
            this.file_info = new HashSet<file_info>();
        }
    
        public int folder_id { get; set; }
        public Nullable<int> parent_id { get; set; }
        public string folder_name { get; set; }
        public string des { get; set; }
        public string keywords { get; set; }
        public bool is_public { get; set; }
        public Nullable<bool> is_sub { get; set; }
        public Nullable<bool> is_upload { get; set; }
        public bool status { get; set; }
        public Nullable<int> is_order { get; set; }
        public Nullable<int> file_number { get; set; }
        public Nullable<int> views { get; set; }
        public Nullable<double> capacity { get; set; }
        public string created_by { get; set; }
        public Nullable<System.DateTime> created_date { get; set; }
        public string created_ip { get; set; }
        public string created_token_id { get; set; }
        public string modified_by { get; set; }
        public Nullable<System.DateTime> modified_date { get; set; }
        public string modified_token_id { get; set; }
        public string modified_ip { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<file_info> file_info { get; set; }
    }
}
