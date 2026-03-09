using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Repositories.Models
{
    public class ReportdModel
    {
         public EmployeeModel Employee { get; set; }

        public int TotalPresent { get; set; }

        public int TotalAbsent { get; set; }

        public int TotalHours { get; set; }

        public List<AttendanceModel> AttendanceList { get; set; }
    }
}