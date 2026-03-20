using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MVCAttendEase.Filters;
using MVCAttendEase.Services;
using Npgsql;
using Repositories.Interfaces;
using Repositories.Models;

namespace MVCAttendEase.Controllers
{
    [Route("[controller]")]
    [ServiceFilter(typeof(EmployeeFilter))]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class EmployeeController : Controller
    {
        private readonly IEmployeeInterface _repo;
        private readonly ILogger<EmployeeController> _logger;
        private readonly IEmployeeInterface _emp;
        private readonly CloudinaryService _cloudinaryService;
        private readonly ElasticsearchService _elastic;

        public EmployeeController(ILogger<EmployeeController> logger, IEmployeeInterface emp, CloudinaryService cloudinaryService, ElasticsearchService elastic)
        {
            _logger = logger;
            _emp = emp;
            _cloudinaryService = cloudinaryService;
            _elastic = elastic;
        }

        private int GetCurrentEmpId()
        {
            return int.TryParse(HttpContext.Session.GetString("empId"), out var empId) ? empId : 0;
        }

        [HttpGet("Dashboard")]
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
            return Ok(new { success = true, data = employee });
        }

        [Route("GetEmployee/{id}")]
        public async Task<IActionResult> GetEmployee(int id)
        {
            EmployeeModel employee = new EmployeeModel();
            employee = await _emp.GetOne(id);
            return Ok(new { success = true, data = employee, message = "Data Fetched Successfully" });
        }


        [Route("ChangePassword/{id}")]
        [HttpPut]
        public async Task<IActionResult> changePwd([FromBody] string password, int id)
        {
            if (password == null)
            {
                return BadRequest("Password has No Value");
            }
            var status = await _emp.ChangePWD(id, password);
            if (status == 1)
            {
                return Ok(new { success = true, message = "Password Changed Successfully...!" });
            }
            else
            {
                return BadRequest(new { message = "There Is Some Error.", success = true });
            }
        }


        [HttpPost]
        [Route("UpdateEmployee")]
        public async Task<IActionResult> UpdateEmployee([FromForm] UpdateEmployee emp)
        {
            if (emp.ProfileImage != null && emp.ProfileImage.Length > 0)
            {
                emp.ProfileImageUrl = await _cloudinaryService.UploadImageAsync(emp.ProfileImage);
            }

            var employee = await _emp.Update(emp);
            if (employee > 0)
            {
                return Ok(new { message = "Update Employee`s Details Successfull", success = true });
            }
            else
            {
                return BadRequest(new { message = "Update Employee Data Failed", success = false });
            }

        }



        [HttpGet("GetAttendence/{empId}")]
        public async Task<IActionResult> GetAttendance(int empId)
        {
            int month = DateTime.Now.Month;
            int year = DateTime.Now.Year;

            var data = await _emp.GetAttendanceByEmployee(empId, year);

            return Ok(data);
        }



        [HttpGet("GetMonthlyWorkingHours")]
        public async Task<IActionResult> GetMonthlyWorkingHours(int empId, int month, int year)
        {
            var data = await _emp.GetMonthlyWorkingHours(empId, month, year);

            return Ok(data);
        }

        [HttpGet("GetYearlyWorkingHours")]
        public async Task<IActionResult> GetYearlyWorkingHours(int empId, int year)
        {
            var data = await _emp.GetYearlyWorkingHours(empId, year);
            return Ok(data);
        }





        [HttpGet("Report")]
        public IActionResult Report()
        {
            int empId = GetCurrentEmpId();
            if (empId <= 0)
            {
                return RedirectToAction("Login", "Auth");
            }

            var dashboard = _emp.GetReportData(empId);

            return View(dashboard);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetAttendances(
            int empId,
            string? searchText,
            DateTime? fromDate,
            DateTime? toDate,
            string? status,
            string? workType)
        {
            if (empId <= 0) empId = GetCurrentEmpId();

            // Always start from DB to keep report data complete and accurate.
            var dbData = _emp.GetAttendanceByEmployee(empId).AsEnumerable();

            if (fromDate.HasValue)
            {
                var from = fromDate.Value.Date;
                dbData = dbData.Where(x => x.AttendDate.HasValue && x.AttendDate.Value.Date >= from);
            }

            if (toDate.HasValue)
            {
                var to = toDate.Value.Date;
                dbData = dbData.Where(x => x.AttendDate.HasValue && x.AttendDate.Value.Date <= to);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                dbData = dbData.Where(x => string.Equals(x.AttendStatus, status, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(workType))
            {
                dbData = dbData.Where(x => string.Equals(x.WorkType, workType, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                var attendanceIds = await _elastic.SearchAttendanceIdsAsync(searchText, empId);
                if (attendanceIds.Count == 0)
                {
                    return Json(Array.Empty<object>());
                }

                dbData = dbData.Where(x => attendanceIds.Contains(x.AttendId));
            }

            var mapped = dbData.Select(x => new
            {
                attendDate = x.AttendDate,
                clockInHour = x.ClockInHour,
                clockInMin = x.ClockInMin,
                clockOutHour = x.ClockOutHour,
                clockOutMin = x.ClockOutMin,
                workingHour = x.WorkingHour,
                workType = x.WorkType,
                taskType = x.TaskType,
                attendStatus = x.AttendStatus
            });

            return Json(mapped);
        }

        // In your EmployeeController.cs

        [HttpGet("[action]")]
        public async Task<IActionResult> SearchAttendance(
    int empId,
    string? searchText,
    DateTime? fromDate,
    DateTime? toDate,
    string? status,
    string? workType)
        {
            return await GetAttendances(empId, searchText, fromDate, toDate, status, workType);
        }

        [HttpGet("[action]")]
        public JsonResult GetReportYearData(int empId, int year)
        {
            if (empId <= 0) empId = GetCurrentEmpId();
            var data = _emp.GetReportYearData(empId, year);

            return Json(data);
        }

        [HttpGet("[action]")]
        public JsonResult GetAttendanceYears(int empId)
        {
            if (empId <= 0) empId = GetCurrentEmpId();
            var years = _emp.GetAttendanceYears(empId);

            return Json(years);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}