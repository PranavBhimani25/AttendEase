using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MVCAttendEase.Services;
using Repositories.Interfaces;
using Repositories.Models;
using MVCAttendEase.Filters;

namespace MVCAttendEase.Controllers
{
    [ServiceFilter(typeof(EmployeeFilter))]
    public class AttendanceController : Controller
    {
        private readonly IAttendanceInterface _attendanceRepo;
        private readonly ElasticsearchService _elastic;
        private readonly ILogger<AttendanceController> _logger;

        [ActivatorUtilitiesConstructor]
        public AttendanceController(
            IAttendanceInterface attendanceRepo,
            ElasticsearchService elastic,
            ILogger<AttendanceController> logger)
        {
            _attendanceRepo = attendanceRepo;
            _elastic = elastic;
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
                await IndexTodayAttendanceToElastic(model.EmpId);
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

            if (result.success)
            {
                await IndexTodayAttendanceToElastic(model.EmpId);
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