using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RollCall.Models
{
    public class MeetingViewModel
    {
        public RCS_MEETING NewMeeting { get; set; }
        public IEnumerable<RCS_MEETING> MeetingList { get; set; }
    }
}