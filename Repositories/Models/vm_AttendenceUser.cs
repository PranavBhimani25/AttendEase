using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Repositories.Models
{
    public class vm_AttendenceUser
    {
        public int AttendID { get; set; }
        public int EmpID { get; set; }
        public DateOnly AttendDate { get; set; }
        public string Status { get; set; }  
    }
}