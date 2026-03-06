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
        private readonly ILogger<AdminController> _logger;


        private readonly IAdminInterface _adminRepo;

       
        public AdminController(ILogger<AdminController> logger,IAdminInterface adminRepo)
        {
            _logger = logger;
             _adminRepo = adminRepo;
        }

        [Route("Dashboard")]
        public IActionResult Dashboard()
        {
            return View();
        }


        [Route("Report")]
         public IActionResult Report()
        {
            return View();
        }

         [HttpGet("GetEmployees")]
        public async Task<IActionResult> GetEmployees()
        {
            var employees = await _adminRepo.GetEmployeesForReport();
            return Ok(employees);
        }

        [HttpGet("GetEmployeeDetails")]
        public async Task<IActionResult> GetEmployeeDetails(int empId)
        {
            if (empId <= 0)
            {
                return BadRequest(new { message = "Invalid employee id." });
            }

            var employee = await _adminRepo.GetEmployeeDetails(empId);

            if (employee == null || employee.EmpId == 0)
            {
                return NotFound(new { message = "Employee not found." });
            }

            return Ok(employee);
        }

        [HttpGet("GetEmployeeMonthlyReportData")]
        public async Task<IActionResult> GetEmployeeMonthlyReportData(int empId, int month, int year)
        {
            if (empId <= 0 || month < 1 || month > 12 || year <= 0)
            {
                return BadRequest(new { message = "Invalid report parameters." });
            }

            var reportData = await _adminRepo.GetEmployeeMonthlyReportData(empId, month, year);
            return Ok(reportData);
        }



        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}
