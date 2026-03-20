using System;
using System.Collections.Generic;
using Repositories.Models;

namespace Repositories.Interfaces
{
    public interface IReportRepository
    {
        List<EmployeeReportItem> GetEmployeesForReport();
        EmployeeReportItem? GetEmployeeById(int empid);
        List<AttendanceReport> GetMonthlyReport(int empid, int month, int year);
        List<AttendanceReport> GetYearlyReport(int empid, int year);
    }
}
