using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Repositories.Models
{
    public class ReportRequestMessage
    {
        public int EmployeeId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email    { get; set; } = string.Empty;
        public int Month       { get; set; }   // e.g. 3 for March
        public int Year        { get; set; }   // e.g. 2026
        public string MonthName { get; set; } = string.Empty; // e.g. "March"
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    }
}