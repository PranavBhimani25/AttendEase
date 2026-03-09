namespace Repositories.Models
{
    public class YearlyWorkingHourModel
    {
        public int MonthNo { get; set; }
        public string MonthLabel { get; set; } = string.Empty;
        public decimal WorkingHours { get; set; }
    }
}
