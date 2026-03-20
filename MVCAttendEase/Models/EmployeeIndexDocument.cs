using System;
using System.Text.Json.Serialization;

namespace MVCAttendEase.Models
{
    public class EmployeeIndexDocument
    {
        [JsonPropertyName("empId")]
        public int EmpId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("gender")]
        public string Gender { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("totalWorkingHours")]
        public int TotalWorkingHours { get; set; }

        [JsonPropertyName("totalDaysPresent")]
        public int TotalDaysPresent { get; set; }

        [JsonPropertyName("lateInCount")]
        public int LateInCount { get; set; }

        [JsonPropertyName("earlyOutCount")]
        public int EarlyOutCount { get; set; }

        [JsonPropertyName("lastAttendDate")]
        public DateTime LastAttendDate { get; set; }
    }
}
