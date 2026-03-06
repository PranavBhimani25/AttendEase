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
            return Ok(new{success=true,data=employees});
        }





        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}