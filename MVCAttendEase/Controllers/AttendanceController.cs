using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MVCAttendEase.Services;
using Repositories.Interfaces;
using Repositories.Models;
using MVCAttendEase.Filters;
using MVCAttendEase.Services;

namespace MVCAttendEase.Controllers
{
    [ServiceFilter(typeof(EmployeeFilter))]
    public class AttendanceController : Controller
    {
        private readonly IAttendanceInterface _attendanceRepo;
        private readonly ElasticsearchService _elastic;
        private readonly ILogger<AttendanceController> _logger;
        private readonly IAdminInterface _adminRepo;

        [ActivatorUtilitiesConstructor]

        public AttendanceController(
            IAttendanceInterface attendanceRepo,
            ElasticsearchService elastic,
            IAdminInterface adminRepo,
            ILogger<AttendanceController> logger)
        {
            _attendanceRepo = attendanceRepo;
            _elastic = elastic;
            _adminRepo = adminRepo;
            _logger = logger;
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

                await _elastic.IndexAttendanceAsync(new AdminReportSearchModel
                {
                    AttendId = model.AttendId, // ⚠️ ensure this is returned from DB
                    EmpId = model.EmpId,
                    EmployeeName = emp.Name,
                    AttendDate = model.AttendDate,
                    AttendStatus = result.status,
                    WorkType = model.WorkType,
                    TaskType = model.TaskType
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

            // 🔥 UPDATE INDEX AFTER CHECKOUT
            if (result.success)
            {
                var emp = await _adminRepo.GetEmployeeDetails(model.EmpId);

                await _elastic.IndexAttendanceAsync(new AdminReportSearchModel
                {
                    AttendId = model.AttendId,
                    EmpId = model.EmpId,
                    EmployeeName = emp.Name,
                    AttendDate = model.AttendDate,
                    AttendStatus = result.status,
                    WorkType = model.WorkType,
                    TaskType = model.TaskType
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
            var result = await _attendanceRepo.GetAttendanceByEmployee(empId);
            return Json(result);
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