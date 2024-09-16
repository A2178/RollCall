using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RollCall.Models
{
    public class CallLogViewModel
    {
        public string MemberName { get; set; }  // 成員名稱
        public int Round { get; set; }          // 抽籤輪次
        public DateTime CreateTime { get; set; } // 抽籤時間
    }
}