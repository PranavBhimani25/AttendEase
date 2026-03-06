namespace Repositories.Models
{
    public class AdminMonthlyReportDataModel
    {
        public MonthlyReportModel Summary { get; set; } = new MonthlyReportModel();
        public List<DailyWorkingHourModel> DailyHours { get; set; } = new List<DailyWorkingHourModel>();
        public List<MonthlyAttendanceDetailModel> AttendanceDetails { get; set; } = new List<MonthlyAttendanceDetailModel>();
    }
}
