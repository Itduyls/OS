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
    
    public partial class doc_master
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public doc_master()
        {
            this.doc_completed_confirms = new HashSet<doc_completed_confirms>();
            this.doc_email_sent = new HashSet<doc_email_sent>();
            this.doc_file_search = new HashSet<doc_file_search>();
            this.doc_files = new HashSet<doc_files>();
            this.doc_follows = new HashSet<doc_follows>();
            this.doc_logs = new HashSet<doc_logs>();
            this.doc_sign_approval = new HashSet<doc_sign_approval>();
            this.doc_tags = new HashSet<doc_tags>();
            this.doc_views = new HashSet<doc_views>();
        }
    
        public int doc_master_id { get; set; }
        public string compendium { get; set; }
        public Nullable<int> nav_type { get; set; }
        public string doc_code { get; set; }
        public Nullable<System.DateTime> doc_date { get; set; }
        public Nullable<int> organization_id { get; set; }
        public Nullable<System.DateTime> receive_date { get; set; }
        public string doc_type { get; set; }
        public string doc_group { get; set; }
        public string urgency { get; set; }
        public string security { get; set; }
        public string issue_place { get; set; }
        public string field { get; set; }
        public string receive_place { get; set; }
        public string send_way { get; set; }
        public string signer { get; set; }
        public string position { get; set; }
        public string tags { get; set; }
        public Nullable<int> num_of_pages { get; set; }
        public Nullable<int> num_of_copies { get; set; }
        public Nullable<System.DateTime> deadline_date { get; set; }
        public Nullable<double> dispatch_book_num { get; set; }
        public string dispatch_book_code { get; set; }
        public Nullable<bool> is_auto_num { get; set; }
        public Nullable<int> created_by { get; set; }
        public Nullable<int> sent_by { get; set; }
        public Nullable<int> handle_by { get; set; }
        public Nullable<System.DateTime> handle_date { get; set; }
        public Nullable<bool> is_drafted { get; set; }
        public Nullable<int> department_id { get; set; }
        public string file_name { get; set; }
        public string file_path { get; set; }
        public string file_path_temp { get; set; }
        public Nullable<double> file_size { get; set; }
        public string file_type { get; set; }
        public Nullable<int> first_doc_status_id { get; set; }
        public Nullable<int> doc_status_id { get; set; }
        public string follow_id { get; set; }
        public Nullable<System.DateTime> effective_date { get; set; }
        public Nullable<System.DateTime> expiration_date { get; set; }
        public Nullable<int> workflow_id { get; set; }
        public Nullable<int> workflow_group_id { get; set; }
        public Nullable<bool> is_inworkflow { get; set; }
        public Nullable<int> workflow_last_person { get; set; }
        public Nullable<int> group_id { get; set; }
        public Nullable<int> leader { get; set; }
        public string warehouse { get; set; }
        public string cabinet { get; set; }
        public string shelf { get; set; }
        public string box { get; set; }
        public string notes { get; set; }
        public string message { get; set; }
        public string ldt { get; set; }
        public string saodv { get; set; }
        public string related_unit { get; set; }
        public string composing_unit { get; set; }
        public Nullable<int> num_of_saved { get; set; }
        public Nullable<int> num_of_issue { get; set; }
        public string drafter { get; set; }
        public Nullable<bool> is_approved { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<doc_completed_confirms> doc_completed_confirms { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<doc_email_sent> doc_email_sent { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<doc_file_search> doc_file_search { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<doc_files> doc_files { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<doc_follows> doc_follows { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<doc_logs> doc_logs { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<doc_sign_approval> doc_sign_approval { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<doc_tags> doc_tags { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<doc_views> doc_views { get; set; }
    }
}
