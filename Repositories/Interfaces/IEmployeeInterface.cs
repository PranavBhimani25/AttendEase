using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Repositories.Models;

namespace Repositories.Interfaces
{
    public interface IEmployeeInterface
    {
        
        Task<EmployeeModel> getEmployeeDetails(int id);

        Task<EmployeeModel> GetOne(int id);

        Task<int> ChangePWD(int id,string pass);

        Task<int> Update(UpdateEmployee emp);

        Task<List<vm_AttendenceUser>> GetAttendanceByEmployee(int empId,int year);

        Task<List<vm_YearlyWorkingHours>> GetYearlyWorkingHours(int empId,int year);

        Task<List<vm_MonthlyWorkingHours>> GetMonthlyWorkingHours(int empId,int month,int year);
        EmployeeModel GetEmployee(int empId);

        ReportdModel GetReportData(int empId);

        List<AttendanceModel> GetAttendanceByEmployee(int empId);
        ReportYearModel GetReportYearData(int empId, int year);
        List<int> GetAttendanceYears(int empId);
    }
}