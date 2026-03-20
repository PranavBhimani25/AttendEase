using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Repositories.Models
{
    public class AttendanceReportVM
    {
        public int EmpID { get; set; }

        public DateTime AttendDate { get; set; }

        public int ClockInHour { get; set; }

        public int ClockInMin { get; set; }

        public int ClockOutHour { get; set; }

        public int ClockOutMin { get; set; }

        public int WorkingHour { get; set; }

        public string AttendStatus { get; set; }

        public string WorkType { get; set; }

        public string TaskType { get; set; }
    }
}