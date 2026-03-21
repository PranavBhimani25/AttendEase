using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MVCAttendEase.Services;
using Repositories.Interfaces;
using Repositories.Models;
using MVCAttendEase.Models;
using MVCAttendEase.Filters;

namespace MVCAttendEase.Controllers
{
    [ServiceFilter(typeof(EmployeeFilter))]
    public class AttendanceController : Controller
    {
        private readonly IAttendanceInterface _attendanceRepo;
        private readonly ElasticsearchService _elastic;
        private readonly ILogger<AttendanceController> _logger;
        private readonly IAdminInterface _adminRepo;
        private readonly RedisService _redis;
        private readonly NotificationPublisher _notificationPublisher; //admin notification

        [ActivatorUtilitiesConstructor]

        public AttendanceController(
            IAttendanceInterface attendanceRepo,
            ElasticsearchService elastic,
            IAdminInterface adminRepo,
            ILogger<AttendanceController> logger,
            RedisService redis,
            NotificationPublisher notificationPublisher)
        {
            _attendanceRepo = attendanceRepo;
            _elastic = elastic;
            _adminRepo = adminRepo;
            _logger = logger;
            _redis = redis;
            _notificationPublisher = notificationPublisher;
        }

        public IActionResult Index()
        {
            return View();
        }

        // ================= CHECK IN =================

        [HttpPost]
        public async Task<IActionResult> CheckIn([FromForm] AttendanceModel model)
        {
            var result = await _attendanceRepo.CheckIn(model);
            if (result.success)
            {
                var emp = await _adminRepo.GetEmployeeDetails(model.EmpId);
                 var empName  = HttpContext.Session.GetString("empName")  ?? "Employee";
                var empEmail = HttpContext.Session.GetString("empEmail") ?? string.Empty;
                var notification = new NotificationMessage
                {
                    EmployeeId       = model.EmpId,
                    FullName         = empName,
                    Email            = empEmail,
                    Role             = "Employee",
                    NotificationType = "CheckIn",
                    Message          = $"{empName} checked in ({result.status}).",
                    RegisteredAt     = DateTime.UtcNow
                };
                try
                {
                    await _notificationPublisher.PublishAttendanceAsync(notification);
                }
                catch (Exception ex)
                {
                    // Don't block the check-in if notification fails
                    _ = ex;
                }
                // Real-time invalidation: wipe every emp:{empId}:* key
                await _redis.InvalidateEmployeeAsync(model.EmpId);
            

                await _elastic.IndexAttendanceAsync(new AdminReportSearchModel
                {
                    AttendId = result.attendId,
                    EmpId = model.EmpId,
                    EmployeeName = emp.Name,
                    AttendDate = model.AttendDate ?? DateTime.Today,
                    AttendStatus = result.status,
                    WorkType = model.WorkType ?? string.Empty,
                    TaskType = model.TaskType ?? string.Empty
                });
            }
            return Json(new
            {
                success = result.success,
                message = result.message,
                status = result.status
            });
        }

        // ================= CHECK OUT =================

        [HttpPost]
        public async Task<IActionResult> CheckOut([FromForm] AttendanceModel model)
        {
            var result = await _attendanceRepo.CheckOut(model);
            //Redis Cache Code
            // If check-out is successful, invalidate all related cache keys for this employee
            if (result.success)
            {
                var empName  = HttpContext.Session.GetString("empName")  ?? "Employee";
                var empEmail = HttpContext.Session.GetString("empEmail") ?? string.Empty;

                var notification = new NotificationMessage
                {
                    EmployeeId       = model.EmpId,
                    FullName         = empName,
                    Email            = empEmail,
                    Role             = "Employee",
                    NotificationType = "CheckOut",
                    Message          = $"{empName} checked out. Worked {result.workingHours}h ({result.status}).",
                    RegisteredAt     = DateTime.UtcNow
                };

                try
                {
                    await _notificationPublisher.PublishAttendanceAsync(notification);
                }
                catch (Exception ex)
                {
                    _ = ex;
                }
         
                // Real-time invalidation: wipe every emp:{empId}:* key
                await _redis.InvalidateEmployeeAsync(model.EmpId);
            }

            // 🔥 UPDATE INDEX AFTER CHECKOUT
            if (result.success)
            {
                var emp = await _adminRepo.GetEmployeeDetails(model.EmpId);

                await _elastic.IndexAttendanceAsync(new AdminReportSearchModel
                {
                    AttendId = result.attendId,
                    EmpId = model.EmpId,
                    EmployeeName = emp.Name,
                    AttendDate = model.AttendDate ?? DateTime.Today,
                    AttendStatus = result.status,
                    WorkType = model.WorkType ?? string.Empty,
                    TaskType = model.TaskType ?? string.Empty
                });
            }

            return Json(new
            {
                success = result.success,
                message = result.message,
                workingHours = result.workingHours,
                status = result.status
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetAttendanceByEmployee(int empId)
        {
            
            //Redis Cache Code for Attendance Grid
            var result = await _redis.GetOrSetAsync(
                RedisKeys.AttendanceGrid(empId),
                async () => await _attendanceRepo.GetAttendanceByEmployee(empId),
                System.TimeSpan.FromMinutes(5));

            return Json(result);
            // var result = await _attendanceRepo.GetAttendanceByEmployee(empId);
            // return Json(result);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error");
        }

        private async Task IndexTodayAttendanceToElastic(int empId)
        {
            try
            {
                var attendances = await _attendanceRepo.GetAttendanceByEmployee(empId);
                var todayAttendance = attendances.FirstOrDefault(x =>
                    x.AttendDate.HasValue && x.AttendDate.Value.Date == DateTime.Today);

                if (todayAttendance == null)
                {
                    return;
                }

                var document = new EmployeeWork
                {
                    AttendId = todayAttendance.AttendId,
                    EmpId = todayAttendance.EmpId,
                    AttendDate = todayAttendance.AttendDate,
                    ClockInHour = todayAttendance.ClockInHour,
                    ClockInMin = todayAttendance.ClockInMin,
                    ClockOutHour = todayAttendance.ClockOutHour,
                    ClockOutMin = todayAttendance.ClockOutMin,
                    WorkingHour = todayAttendance.WorkingHour,
                    AttendStatus = todayAttendance.AttendStatus,
                    WorkType = todayAttendance.WorkType,
                    TaskType = todayAttendance.TaskType
                };

                await _elastic.IndexAttendanceAsync(document);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "DB write succeeded, but failed to sync attendance to Elasticsearch for EmpId {EmpId}", empId);
            }
        }
    }
}