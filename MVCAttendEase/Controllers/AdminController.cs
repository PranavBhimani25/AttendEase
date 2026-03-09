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
            return View();
        }

        [Route("GetEmployeeCount")]
        public async Task<IActionResult> GetEmployeeCount()
        {
            var count=await _adminRepo.GetEmployeeCount();
            // Console.WriteLine(count);
            if(count == -1)
            {
                return Ok(new{success=false,message="Employee not found"});
            }
            return Ok(new{success=true,data=count});
        }

        [Route("PresentEmpCount")]
        public async Task<IActionResult> PresentEmpCount()
        {
            var count=await _adminRepo.PresentEmpCount();
            // Console.WriteLine(count);
            if(count == -1)
            {
                return Ok(new{success=false,message="Employee not present"});
            }
            return Ok(new{success=true,data=count});
        }

        [Route("AbsentEmpCount")]   
        public async Task<IActionResult> AbsentEmpCount()
        {
            var count=await _adminRepo.AbsentEmpCount();
            // Console.WriteLine(count);
            if(count == -1)
            {
                return Ok(new{success=false,message="Absent Employee not found"});
            }
            return Ok(new{success=true,data=count});
        }

        [Route("OnLeaveEmpCount")]
        public async Task<IActionResult> OnLeaveEmpCount()
        {
            var count=await _adminRepo.OnLeaveEmpCount();
            // Console.WriteLine(count);
            if(count == -1)
            {
                return Ok(new{success=false,message="Not one on Leave"});
            }
            return Ok(new{success=true,data=count});
        }

        [Route("DevelopingHour")]
        public async Task<IActionResult> DevelopingHour()
        {
            var count=await _adminRepo.CountDevelopingHour();
            // Console.WriteLine(count);
            if(count == -1)
            {
                return Ok(new{success=false,message="Developing hour is not count"});
            }
            return Ok(new{success=true,data=count});
        }

        [Route("DesigningHour")]
        public async Task<IActionResult> DesigningHour()
        {
            var count=await _adminRepo.CountDesigningHour();
            // Console.WriteLine(count);
            if(count == -1)
            {
                return Ok(new{success=false,message="Designing hour is not count"});
            }
            return Ok(new{success=true,data=count});
        }


        [Route("ResearchHour")]
        public async Task<IActionResult> ResearchHour()
        {
            var count=await _adminRepo.CountResearchHour();
            // Console.WriteLine(count);
            if(count == -1)
            {
                return Ok(new{success=false,message="Research hour not found"});
            }
            return Ok(new{success=true,data=count});
        }

        [Route("ListEmployee")]
        public async Task<IActionResult> ListEmployee()
        {
            var employees=await _adminRepo.ListEmployee();
            if(employees == null)
            {
                return Ok(new{success=false,message="Employee not found"});
            }
            return Ok(new{success=true,data=employees,total=employees.Count()});
        }

        [HttpPost]
        [Route("UpdateEmpStatus")]
        public async Task<IActionResult> UpdateEmpStatus(int id,string status)
        {
            var result=await _adminRepo.UpdateEmpStatus(id,status);
            if(result > 0 )
            {
                return Ok(new{success=true,message="Status updated"});
            }
            return Ok(new{success=false,message="Status not updated"});
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
