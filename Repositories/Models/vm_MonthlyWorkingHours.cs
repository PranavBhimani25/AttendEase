using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Repositories.Models
{
    public class vm_MonthlyWorkingHours
    {
        public DateOnly Date { get; set; }
        public int WorkingHour { get; set; }
    }
}