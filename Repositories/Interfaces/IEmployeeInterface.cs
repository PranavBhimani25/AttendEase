using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Repositories.Models;

namespace Repositories.Interfaces
{
    public interface IEmployeeInterface
    {
        EmployeeModel GetEmployee(int empId);

        ReportdModel GetReportData(int empId);

        List<AttendanceModel> GetAttendanceByEmployee(int empId);
        ReportYearModel GetReportYearData(int empId, int year);
        List<int> GetAttendanceYears(int empId);
    }
}