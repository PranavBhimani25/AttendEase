using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Repositories.Models
{
    public class MonthlyReportModel
    {
        public int EmpId { get; set; }
        public string EmpName { get; set; }

        public int PresentDays { get; set; }
        public int AbsentDays { get; set; }
        public int LateInDays { get; set; }
        public int EarlyOutDays { get; set; }
        public int RegularDays { get; set; }
    }
}