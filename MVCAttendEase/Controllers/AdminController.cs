using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MVCAttendEase.Filters;
using Repositories.Interfaces;

namespace MVCAttendEase.Controllers
{
    [Route("[controller]")]
    [ServiceFilter(typeof(AdminFilter))]
    public class AdminController : Controller
    {

        private readonly IAdminInterface _adminRepo;

        public AdminController(IAdminInterface adminRepository)
        {
            _adminRepo=adminRepository;
        }

        [Route("Dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            var count=await _adminRepo.GetEmployeeCount();
            Console.WriteLine(count);
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}