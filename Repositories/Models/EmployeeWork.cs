using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Repositories.Models
{
    public class EmployeeWork
    {
        [JsonPropertyName("c_attendid")]
        public long AttendId { get; set; }

        [JsonPropertyName("c_empid")]
        public long EmpId { get; set; }

        [JsonPropertyName("c_attenddate")]
        public DateTime? AttendDate { get; set; }

        [JsonPropertyName("c_clockinhour")]
        public long? ClockInHour { get; set; }

        [JsonPropertyName("c_clockinmin")]
        public long? ClockInMin { get; set; }

        [JsonPropertyName("c_clockouthour")]
        public long? ClockOutHour { get; set; }

        [JsonPropertyName("c_clockoutmin")]
        public long? ClockOutMin { get; set; }

        [JsonPropertyName("c_workinghour")]
        public long? WorkingHour { get; set; }

        [JsonPropertyName("c_attendstatus")]
        public string? AttendStatus { get; set; }

        [JsonPropertyName("c_worktype")]
        public string? WorkType { get; set; }

        [JsonPropertyName("c_tasktype")]
        public string? TaskType { get; set; }
    }
}