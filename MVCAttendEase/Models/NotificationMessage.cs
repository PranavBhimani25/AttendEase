using System;

namespace MVCAttendEase.Models
{
    public class NotificationMessage
    {
        public int EmployeeId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = "Employee";
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
        public string Message { get; set; } = "New employee registered";
    }
}
