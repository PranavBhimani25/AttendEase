using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MVCAttendEase.Filters;
using MVCAttendEase.Models;
using MVCAttendEase.Services;
using Repositories.Interfaces;
using Repositories.Models;
using StackExchange.Redis;

namespace MVCAttendEase.Controllers
{
    [Route("[controller]")]
    [ServiceFilter(typeof(AdminFilter))]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class AdminController : Controller
    {

        private readonly IAdminInterface _adminRepo;
        private readonly RedisService   _redisService;
        private readonly RabbitMQService _rabbit;
        private readonly ElasticsearchService _elastic;
        private readonly MailService _mailService;

        public AdminController(IAdminInterface adminRepository, RedisService redisService, RabbitMQService rabbit, ElasticsearchService elastic, MailService mailService)
        {
            _adminRepo=adminRepository;
            _redisService = redisService;
            _rabbit = rabbit;
            _elastic = elastic;
            _mailService = mailService;
        }

        [Route("Dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            // await _redisService.SetAsync("test", "This test valueansdbbs ");

            // var msg = new ChatMessage
            // {
            //     Sender = "Pranav",
            //     Receiver = "Dev",
            //     Message = "Hello",
            //     Timestamp = DateTime.UtcNow
            // };

            //  var data = new
            // {
            //     Message = "Hello Elasticsearch 🚀 ajsdbajs",
            //     Time = DateTime.UtcNow
            // };
            // await _elastic.CreateIndexAsync("attendance_logs");

            // await _elastic.IndexAsync(data);

            // await _rabbit.SendMessageAsync("Dev", msg);


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



        [HttpPost("SendReportEmail")]
        public async Task<IActionResult> SendReportEmail([FromBody] SendPdfModel model)
        {
            try
            {
                // Step 1: Get employee email
                var emp = await _adminRepo.GetEmployeeById(model.EmpId);

                if (emp == null)
                {
                    return BadRequest(new { message = "Employee not found" });
                }

                // Step 2: Convert Base64 to byte[]
                byte[] pdfBytes = Convert.FromBase64String(model.FileData);

                string subject = $"Attendance Report - {DateTime.Now:dd MMM yyyy}";
                // ✅ Email body (HTML)
                string path = Path.Combine(Directory.GetCurrentDirectory(), "EmailTemplates", "ReportEmployee.html");
                string body = System.IO.File.ReadAllText(path);
                body = body.Replace("{{FileName}}", model.FileName).Replace("{{Date}}", DateTime.Now.ToString("dd-MM-yyyy"));

                Console.WriteLine(emp.Email);
                // Step 3: Send Email
                _mailService.SendEmail(emp.Email, subject, body, pdfBytes);

                return Ok(new { message = "PDF sent successfully to employee email", success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in Sending Email :- " + ex.Message);
                return BadRequest(new { message = ex.Message });
            }
        }



        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}
