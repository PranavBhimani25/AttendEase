using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Repositories.Models;

namespace Repositories.Interfaces
{
    public interface IAuthInterface
    {
        Task<EmployeeModel> Login(LoginModel model);
        Task<int> Register(RegisterEmployeeModel model);
        Task<string> Logout(string token);
        Task<string> ChangePassword(ChangePasswordModel model);
        Task<int> ForgetPassword(LoginModel model);

        Task<int> GetOne(string email);


    }
}