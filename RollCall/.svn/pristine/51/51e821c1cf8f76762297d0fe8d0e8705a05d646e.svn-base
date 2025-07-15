using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RollCall.Models
{
    public class MeetingEditViewModel
    {
        public RCS_MEETING Meeting { get; set; }

        // 常用自定義群組 (RCS_GROUP) 清單
        // 自定義群組分配
        public List<RCS_GROUP> DailyGroupList { get; set; }

        // 會議組別 (RCS_MEETING_GROUP) 清單
        public List<RCS_MEETING_GROUP> MeetingGroupsList { get; set; }

        // 員工 (RCS_EMPLOYEES) 清單
        // 單獨加入成員下拉
        public List<RCS_EMPLOYEES> EmployeesList { get; set; }

        // 分組＋成員的結構 (一個 RCS_MEETING_GROUP 及其底下的員工)
        // 用於在頁面上一次呈現「與會人員名單 (多個分組)」的資料。
        public List<MeetingGroupViewModel> MeetingGroups { get; set; }
        public List<RCS_GROUP> UsedDailyGroupList { get; set; }

        public List<SingleMemberViewModel> SingleMembers { get; set; }
    }

    // 會議組別 + 底下成員(已 Join 的顯示資料)
    public class MeetingGroupViewModel
    {
        public long AUTO_ID { get; set; }               // RCS_MEETING_GROUP.AUTO_ID
        public string GROUP_NAME { get; set; }

        // 該組別底下的成員 (來自 RCS_MEMBER + RCS_EMPLOYEES)
        public List<MeetingMemberViewModel> Members { get; set; }
    }

    // 成員模型: RCS_MEMBER + RCS_EMPLOYEES
    public class MeetingMemberViewModel
    {
        public long MemberAutoId { get; set; }   // RCS_MEMBER.AUTO_ID
        public long EmpAutoId { get; set; }   // RCS_MEMBER.EMP_AUTO_ID => RCS_EMPLOYEES.AUTO_ID

        public string EmployeeName { get; set; } // 來自 RCS_EMPLOYEES.EMP_NAME
    }

    public class SingleMemberViewModel
    {
        public long EmpId { get; set; }
        public string EmpNumber { get; set; }
        public string EmpName { get; set; }
        public string Department { get; set; }
        public string Position { get; set; }
        public string MeetingGroupName { get; set; }
        public bool IsFromDailyGroup { get; set; } // 新增，用來標示是否來自常用群組
    }


}