using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Repositories.Models
{
    public class ReportYearModel
    {
        public int Present { get; set; }
        public int Absent { get; set; }
        public int TotalHours { get; set; }

        public List<int> PresentMonthly { get; set; }
        public List<int> AbsentMonthly { get; set; }

        public double[] Developing { get; set; } = new double[12];
        public double[] Designing { get; set; } = new double[12];
        public double[] Research { get; set; } = new double[12];

        public int Remote { get; set; }
        public int Office { get; set; }
        public int Field { get; set; }

        public List<int> Regular { get; set; }
        public List<int> LateIn { get; set; }
        public List<int> EarlyOut { get; set; }
        public List<AttendanceModel> GridData { get; set; }
    }
}