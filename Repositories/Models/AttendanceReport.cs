namespace Repositories.Models
{
    public class AttendanceReport
    {
        public DateTime AttendDate { get; set; }
        public int ClockInHour { get; set; }
        public int ClockInMin { get; set; }
        public int ClockOutHour { get; set; }
        public int ClockOutMin { get; set; }
        public int WorkingHour { get; set; }
        public string Status { get; set; }
        public string WorkType { get; set; }
        public string TaskType { get; set; }
    }
}
