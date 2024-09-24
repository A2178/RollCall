using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RollCall.Models
{
    public class MeetingEditViewModel
    {
        public RCS_MEETING Meeting { get; set; }
        public List<RCS_MEMBER> Members { get; set; }
    }
}