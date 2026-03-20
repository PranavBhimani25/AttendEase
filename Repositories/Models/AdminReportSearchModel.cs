using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Repositories.Models
{
    public class AdminReportSearchModel
    {
        public int AttendId { get; set; }
        public int EmpId { get; set; }
        public string EmployeeName { get; set; }  // 🔥 Required for search
        public DateTime? AttendDate { get; set; }
        public string AttendStatus { get; set; }
        public string WorkType { get; set; }
        public string TaskType { get; set; }
    }
}