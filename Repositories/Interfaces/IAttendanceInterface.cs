using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Repositories.Models;

namespace Repositories.Interfaces
{
    public interface IAttendanceInterface
    {
         Task<bool> IsTodayAttendanceExists(int empid);

        Task<(bool success, string message, string status)> CheckIn(AttendanceModel model);

        Task<(bool success, string message, int workingHours, string status)> CheckOut(AttendanceModel model);

        Task<List<AttendanceModel>> GetAttendanceByEmployee(int empId);
    }
}