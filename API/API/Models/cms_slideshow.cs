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
    
    public partial class cms_slideshow
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public cms_slideshow()
        {
            this.cms_slideshow_image = new HashSet<cms_slideshow_image>();
        }
    
        public int slideshow_id { get; set; }
        public Nullable<int> topic_id { get; set; }
        public Nullable<int> organization_id { get; set; }
        public string slideshow_name { get; set; }
        public Nullable<int> is_type { get; set; }
        public int lang_id { get; set; }
        public bool status { get; set; }
        public string keywords { get; set; }
        public int is_order { get; set; }
        public string created_by { get; set; }
        public Nullable<System.DateTime> created_date { get; set; }
        public string created_ip { get; set; }
        public string created_token_id { get; set; }
        public string modified_by { get; set; }
        public Nullable<System.DateTime> modified_date { get; set; }
        public string modified_token_id { get; set; }
        public string modified_ip { get; set; }
    
        public virtual cms_topic cms_topic { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<cms_slideshow_image> cms_slideshow_image { get; set; }
    }
}
