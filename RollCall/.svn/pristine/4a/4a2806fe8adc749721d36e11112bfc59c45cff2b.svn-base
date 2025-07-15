using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RollCall.Models
{
    public class RaffleMemberDto
    {
        public long EmpId { get; set; }     // RCS_MEMBER.EMP_AUTO_ID
        public string EmpName { get; set; } // RCS_EMPLOYEES.EMP_NAME
    }

    public class GroupedRaffleViewModel
    {
        public string GroupName { get; set; }

        // 從 RCS_MEMBER + RCS_EMPLOYEES Join 後，組好的員工清單
        public List<RaffleMemberDto> Members { get; set; }
    }

    public class RaffleViewModel
    {
        public string MeetingName { get; set; }
        public DateTime? MeetingStart { get; set; }
        public DateTime? MeetingEnd { get; set; }
        public DateTime? CurrentTime { get; set; }
        public int CurrentRound { get; set; }
        public Guid MeetingGuid { get; set; }
        public bool IsTestPeriod { get; set; }

        public List<GroupedRaffleViewModel> GroupedMembers { get; set; }

        public string MeetingStartFormatted => MeetingStart?.ToString("yyyy/MM/dd HH:mm:ss") ?? "無資料";
        public string MeetingEndFormatted => MeetingEnd?.ToString("yyyy/MM/dd HH:mm:ss") ?? "無資料";
        public string CurrentTimeFormatted => CurrentTime?.ToString("yyyy/MM/dd HH:mm:ss") ?? "無資料";
    }



}