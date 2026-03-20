using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Repositories.Models
{
    public class EmployeeSearchIndex
    {
         public int EmpId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Gender { get; set; }
        public string Status { get; set; }
        public string Role { get; set; }

        public int TotalWorkingHours { get; set; }
        public int TotalDaysPresent { get; set; }
        public int LateInCount { get; set; }
        public int EarlyOutCount { get; set; }

        public DateTime LastAttendDate { get; set; }
    }
}