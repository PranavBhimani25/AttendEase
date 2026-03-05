using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MVCAttendEase.Filters;
using Repositories.Interfaces;
using Repositories.Models;

namespace MVCAttendEase.Controllers
{
    [Route("[controller]")]
    [ServiceFilter(typeof(EmployeeFilter))]
    public class EmployeeController : Controller
    {
        private readonly ILogger<EmployeeController> _logger;
        private readonly IEmployeeInterface _emp;

        public EmployeeController(ILogger<EmployeeController> logger, IEmployeeInterface emp)
        {
            _logger = logger;
            _emp = emp;
        }

        [Route("Dashboard")]
        public IActionResult Dashboard()
        {
            return View();
        }

        [Route("Profile")]
        public IActionResult Profile()
        {
            return View();
        }


        [Route("GetEmployeeProfile/{id}")]
        public async Task<IActionResult> GetEmployeeProfile(int id)
        {
            EmployeeModel employee = new EmployeeModel();
            employee = await _emp.getEmployeeDetails(id);
            return Ok(new{success = true, data = employee}) ;
        }

        [Route("GetEmployee/{id}")]
        public async Task<IActionResult> GetEmployee(int id)
        {
            EmployeeModel employee = new EmployeeModel();
            employee = await _emp.GetOne(id);
            return Ok(new {success = true, data = employee, message = "Data Fetched Successfully"});
        }


        [Route("ChangePassword/{id}")]
        [HttpPut]
        public async Task<IActionResult> changePwd([FromBody]string password,int id)
        {
            if (password == null)
            {
                return BadRequest("Password has No Value");
            }
            var status=await  _emp.ChangePWD(id,password);
            if (status == 1)
            {
                return Ok(new{success=true,message="Password CHnage SuccessFully...!"});
            }
            else
            {
                return BadRequest("There Is Some Error.");
            }
        }

      

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}