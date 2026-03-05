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
    }
}