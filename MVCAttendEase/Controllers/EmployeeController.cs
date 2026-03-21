using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MVCAttendEase.Filters;
using MVCAttendEase.Models;
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
        private readonly ILogger<EmployeeController> _logger;
        private readonly IEmployeeInterface _emp;
        private readonly CloudinaryService _cloudinaryService;
        private readonly ElasticsearchService _elastic;
        private readonly INotificationInterface _notification;
        private readonly RedisService _redis;
        private readonly NotificationPublisher _notificationPublisher;

        public EmployeeController(ILogger<EmployeeController> logger, IEmployeeInterface emp, CloudinaryService cloudinaryService, ElasticsearchService elastic, INotificationInterface notification, RedisService redis, NotificationPublisher notificationPublisher)
        {
            _logger = logger;
            _emp = emp;
            _cloudinaryService = cloudinaryService;
            _elastic = elastic;
            _notification = notification;
            _redis = redis;
            _notificationPublisher = notificationPublisher;
        }


        private int GetCurrentEmpId()
        {
            return int.TryParse(HttpContext.Session.GetString("empId"), out var empId) ? empId : 0;
        }

        [HttpGet("Dashboard")]
        public async Task<IActionResult> DashboardAsync()
        {
            await using var connection = await _notification.GetConnectionAsync();
            var userId = HttpContext.Session.GetString("empId");
            if (string.IsNullOrWhiteSpace(userId))
            {
                return RedirectToAction("Login", "Auth");
            }
            System.Console.WriteLine("UserName " + userId);

            await _notification.GetEmpNotification(connection, userId);
            return View();
        }

        [HttpGet("GetNotifications")]
        public async Task<IActionResult> GetNotifications()
        {
            var userId = HttpContext.Session.GetString("empId");
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { success = false, message = "Employee session expired." });
            }
            var notifications = await _notification.GetStoredNotifications(userId);

            return Ok(new
            {
                success = true,
                data = notifications
            });
        }

        [HttpPost("MarkNotificationRead")]
        public async Task<IActionResult> MarkNotificationRead([FromBody] MsgToEmp model)
        {
            var userId = HttpContext.Session.GetString("empId");
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { success = false });
            }
            var isRemoved = await _notification.MarkNotificationAsRead(userId, model);

            return Ok(new
            {
                success = isRemoved
            });
        }

        [HttpPost("SendMonthlyReportRequest")]
        public async Task<IActionResult> SendMonthlyReportRequest([FromBody] JsonElement payload)
        {
            if (!payload.TryGetProperty("month", out var monthElement) ||
                !payload.TryGetProperty("year", out var yearElement) ||
                !monthElement.TryGetInt32(out var month) ||
                !yearElement.TryGetInt32(out var year))
            {
                return BadRequest(new { success = false, message = "Invalid month/year selection." });
            }

            if (month < 1 || month > 12 || year < 2000 || year > 2100)
            {
                return BadRequest(new { success = false, message = "Invalid month/year selection." });
            }

            var empId = GetCurrentEmpId();
            if (empId <= 0)
            {
                return Unauthorized(new { success = false, message = "Employee session expired. Please login again." });
            }

            var selectedMonth = new DateTime(year, month, 1);
            var empName = HttpContext.Session.GetString("empName") ?? "Employee";
            var empEmail = HttpContext.Session.GetString("empEmail") ?? string.Empty;

            if (string.IsNullOrWhiteSpace(empEmail) || empName == "Employee")
            {
                var employee = await _emp.GetOne(empId);
                if (employee != null)
                {
                    if (string.IsNullOrWhiteSpace(empName) || empName == "Employee")
                    {
                        empName = employee.Name;
                    }

                    if (string.IsNullOrWhiteSpace(empEmail))
                    {
                        empEmail = employee.Email;
                    }
                }
            }

            var notification = new NotificationMessage
            {
                EmployeeId = empId,
                FullName = empName,
                Email = empEmail,
                Role = "Employee",
                NotificationType = "MonthRequest",
                Message = $"{empName} requested monthly report for {selectedMonth:MMM yyyy}.",
                RegisteredAt = DateTime.UtcNow
            };

            try
            {
                await _notificationPublisher.PublishAttendanceAsync(notification);
                return Ok(new { success = true, message = "Request sent to admin successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send monthly report request for EmpId {EmpId}", empId);
                return StatusCode(500, new { success = false, message = "Failed to send request." });
            }
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

            var data = await _redis.GetOrSetAsync(
                RedisKeys.AttendanceByYear(empId, year),
                async () => await _emp.GetAttendanceByEmployee(empId, year),
                TimeSpan.FromMinutes(5));

            return Ok(data);
        }

        /// Monthly working-hours line chart (one point per day).
        /// Redis key: emp:{empId}:working:monthly:{year}:{month}   TTL: 1 hour
        /// </summary>
        [HttpGet("GetMonthlyWorkingHours")]
        public async Task<IActionResult> GetMonthlyWorkingHours(int empId, int month, int year)
        {
            var data = await _redis.GetOrSetAsync(
                RedisKeys.MonthlyWorkingHours(empId, month, year),
                async () => await _emp.GetMonthlyWorkingHours(empId, month, year),
                TimeSpan.FromMinutes(5));

            return Ok(data);
        }

        /// <summary>
        /// Yearly working-hours bar chart (one bar per month).
        /// Redis key: emp:{empId}:working:yearly:{year}   TTL: 1 hour
        [HttpGet("GetYearlyWorkingHours")]
        public async Task<IActionResult> GetYearlyWorkingHours(int empId, int year)
        {
            var data = await _redis.GetOrSetAsync(
                RedisKeys.YearlyWorkingHours(empId, year),
                async () => await _emp.GetYearlyWorkingHours(empId, year),
                TimeSpan.FromMinutes(5));

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

        /// Report page: grid of all attendance records (no year filter).
        /// Redis key: emp:{empId}:attendance:grid   TTL: 1 hour
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
        // ─────────────────────────────────────────────
        //  Report – chart + grid data  (Redis-cached)
        // ─────────────────────────────────────────────

        [HttpGet("[action]")]
        public async Task<IActionResult> GetReportYearData(int empId, int year)
        {
            if (empId <= 0) empId = GetCurrentEmpId();

            var data = await _redis.GetOrSetAsync(
                RedisKeys.ReportYearData(empId, year),
                () => _emp.GetReportYearData(empId, year),  // sync factory
                TimeSpan.FromMinutes(5));

            return Json(data);
        }

        [HttpGet("[action]")]
        public JsonResult GetAttendanceYears(int empId)
        {
            if (empId <= 0) empId = GetCurrentEmpId();
            var years = _emp.GetAttendanceYears(empId);

            return Json(years);
        }

        // ─────────────────────────────────────────────
        //  Dashboard – chart data  (Redis-cached)
        // ─────────────────────────────────────────────

        /// <summary>
        /// Employee Dashboard: summary cards + attendance list for the current month.
        /// Redis key: emp:{empId}:dashboard   TTL: 5 minutes
        /// Invalidated automatically when attendance is written (CheckIn/CheckOut).
        /// </summary>
        [HttpGet("GetDashboardData")]
        public async Task<IActionResult> GetDashboardData(int empId)
        {
            if (empId <= 0) empId = GetCurrentEmpId();

            var data = await _redis.GetOrSetAsync(
                RedisKeys.DashboardData(empId),
                async () => await Task.FromResult(_emp.GetReportData(empId)), // FIX: async overload — key always saved
                TimeSpan.FromMinutes(5));
            return Ok(new { success = true, data });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}