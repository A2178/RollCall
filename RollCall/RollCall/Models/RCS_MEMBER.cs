//------------------------------------------------------------------------------
// <auto-generated>
//     這個程式碼是由範本產生。
//
//     對這個檔案進行手動變更可能導致您的應用程式產生未預期的行為。
//     如果重新產生程式碼，將會覆寫對這個檔案的手動變更。
// </auto-generated>
//------------------------------------------------------------------------------

namespace RollCall.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class RCS_MEMBER
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public RCS_MEMBER()
        {
            this.RCS_CALL_LOG = new HashSet<RCS_CALL_LOG>();
        }
    
        public long AUTO_ID { get; set; }
        public System.Guid AUTO_GUID { get; set; }
        public long MEETING_AUTO_ID { get; set; }
        public string MEMBER_NAME { get; set; }
        public string REMARK { get; set; }
        public bool IS_ACTIVED { get; set; }
        public string CREATE_BY { get; set; }
        public System.DateTime CREATE_TIME { get; set; }
        public string MODIFY_BY { get; set; }
        public System.DateTime MODIFY_TIME { get; set; }
        public bool IS_DELETED { get; set; }
        public string DELETE_BY { get; set; }
        public Nullable<System.DateTime> DELETE_TIME { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<RCS_CALL_LOG> RCS_CALL_LOG { get; set; }
        public virtual RCS_MEETING RCS_MEETING { get; set; }
    }
}
