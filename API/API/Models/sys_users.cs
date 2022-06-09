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
    
    public partial class sys_users
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public sys_users()
        {
            this.sys_fire_base = new HashSet<sys_fire_base>();
            this.sys_sendhub = new HashSet<sys_sendhub>();
            this.sys_token = new HashSet<sys_token>();
            this.sys_web_acess = new HashSet<sys_web_acess>();
        }
    
        public string user_id { get; set; }
        public int user_key { get; set; }
        public Nullable<int> organization_id { get; set; }
        public Nullable<int> department_id { get; set; }
        public string avatar { get; set; }
        public string is_password { get; set; }
        public string full_name { get; set; }
        public string phone { get; set; }
        public string email { get; set; }
        public string key_encript { get; set; }
        public Nullable<int> wrong_pass_count { get; set; }
        public Nullable<int> position_id { get; set; }
        public int is_order { get; set; }
        public int status { get; set; }
        public string ip { get; set; }
        public string token_id { get; set; }
        public string role_id { get; set; }
        public bool is_admin { get; set; }
        public string app_modules { get; set; }
        public string created_by { get; set; }
        public Nullable<System.DateTime> created_date { get; set; }
        public string created_ip { get; set; }
        public string created_token_id { get; set; }
        public string modified_by { get; set; }
        public Nullable<System.DateTime> modified_date { get; set; }
        public string modified_token_id { get; set; }
        public string modified_ip { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<sys_fire_base> sys_fire_base { get; set; }
        public virtual sys_roles sys_roles { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<sys_sendhub> sys_sendhub { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<sys_token> sys_token { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<sys_web_acess> sys_web_acess { get; set; }
    }
}
