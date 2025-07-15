using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RollCall.Models
{
    public class RaffleViewModel
    {
        public string MeetingName { get; set; }
        public DateTime? MeetingStart { get; set; }
        public DateTime? MeetingEnd { get; set; }
        public DateTime? CurrentTime { get; set; }
        public int CurrentRound { get; set; }

        // 添加 MeetingGuid 属性
        public Guid MeetingGuid { get; set; }

        // Members 列表
        public List<RCS_MEMBER> Members { get; set; }

        public string MeetingStartFormatted => MeetingStart.HasValue ? MeetingStart.Value.ToString("yyyy/MM/dd HH:mm:ss") : "無資料";
        public string MeetingEndFormatted => MeetingEnd.HasValue ? MeetingEnd.Value.ToString("yyyy/MM/dd HH:mm:ss") : "無資料";
        public string CurrentTimeFormatted => CurrentTime.HasValue ? CurrentTime.Value.ToString("yyyy/MM/dd HH:mm:ss") : "無資料";
    }

}