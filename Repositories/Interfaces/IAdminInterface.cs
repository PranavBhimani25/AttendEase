using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Repositories.Models;

namespace Repositories.Interfaces
{
    public interface IAdminInterface
    {
        Task<int> GetEmployeeCount();

        Task<int> PresentEmpCount();

        Task<int> AbsentEmpCount();

        Task<int> OnLeaveEmpCount();

        Task<int> CountDevelopingHour();
        Task<int> CountDesigningHour();
        Task<int> CountResearchHour();

        Task<List<EmployeeModel>> ListEmployee();

        Task<int> UpdateEmpStatus();
    }
}