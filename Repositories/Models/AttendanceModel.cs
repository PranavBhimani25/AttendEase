using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;



namespace Repositories.Models
{
    public class AttendanceModel
    {
        public int AttendId { get; set; }

        [Required(ErrorMessage = "Employee Id is required")]
        public int EmpId { get; set; }

        [Required(ErrorMessage = "Attendance date is required")]
        public DateTime? AttendDate { get; set; }

        [Required(ErrorMessage = "Clock-in hour is required")]
        [Range(0, 23, ErrorMessage = "Clock-in hour must be between 0 and 23")]
        public int? ClockInHour { get; set; }

        [Required(ErrorMessage = "Clock-in minutes are required")]
        [Range(0, 59, ErrorMessage = "Clock-in minutes must be between 0 and 59")]
        public int? ClockInMin { get; set; }

        [Range(0, 23, ErrorMessage = "Clock-out hour must be between 0 and 23")]
        public int? ClockOutHour { get; set; }

        [Range(0, 59, ErrorMessage = "Clock-out minutes must be between 0 and 59")]
        public int? ClockOutMin { get; set; }

        [Range(0, 24, ErrorMessage = "Working hours must be between 0 and 24")]
        public int? WorkingHour { get; set; }

        [Required(ErrorMessage = "Attendance status is required")]
        [RegularExpression("^(LateIn|EarlyOut|Regular)$",
            ErrorMessage = "Status must be LateIn, EarlyOut, or Regular")]
        public string? AttendStatus { get; set; }

        [Required(ErrorMessage = "Work type is required")]
        public string? WorkType { get; set; }

        public string? TaskType { get; set; }
    }
}