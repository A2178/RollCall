using PagedList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RollCall.Models
{
    public class EmployeeViewModel
    {
        public IPagedList<RCS_EMPLOYEES> EmployeeList { get; set; }
        public long AutoId { get; set; }
        public string EmpName { get; set; }
        public string Department {  get; set; }
        public string Position { get; set; }
        public string EmpNumber { get; set; }
        public string SalesDepID { get; set; }
        public DateTime? Modify_Time { get; set; }
    }
}