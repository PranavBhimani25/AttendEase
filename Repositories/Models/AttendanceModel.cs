using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Repositories.Models
{
    public class AttendanceModel
    {
        public int c_attendid { get; set; }

        public int c_empid { get; set; }

        public DateTime c_attenddate { get; set; }

        public int c_clockinhour { get; set; }

        public int c_clockinmin { get; set; }

        public int? c_clockouthour { get; set; }

        public int? c_clockoutmin { get; set; }

        public int? c_workinghour { get; set; }

        public string c_attendstatus { get; set; }

        public string c_worktype { get; set; }

        public string? c_tasktype { get; set; }
    }
}