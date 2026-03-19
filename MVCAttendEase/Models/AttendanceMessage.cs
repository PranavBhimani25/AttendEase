using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MVCAttendEase.Models
{
    public class AttendanceMessage
    {
        public int UserId { get; set; }
        public string Name { get; set; }
        public DateTime Timestamp { get; set; }
        public string Status { get; set; } // CheckIn / CheckOut
    }
}