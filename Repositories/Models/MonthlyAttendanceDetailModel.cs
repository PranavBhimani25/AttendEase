namespace Repositories.Models
{
    public class MonthlyAttendanceDetailModel
    {
        public DateTime AttendDate { get; set; }
        public string WorkType { get; set; } = string.Empty;
        public string TaskType { get; set; } = string.Empty;
        public decimal WorkingHours { get; set; }
        public string AttendStatus { get; set; } = string.Empty;
    }
}
