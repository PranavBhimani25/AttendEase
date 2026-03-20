using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
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
        private readonly RedisService _redis;

        public AttendanceController(IAttendanceInterface attendanceRepo, RedisService redis)
        {
            _attendanceRepo = attendanceRepo;
            _redis = redis;
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
            
            //Redis Cache Code
            // If check-in is successful, invalidate all related cache keys for this employee
            if (result.success)
            {
                // Real-time invalidation: wipe every emp:{empId}:* key
                await _redis.InvalidateEmployeeAsync(model.EmpId);
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
                // Real-time invalidation: wipe every emp:{empId}:* key
                await _redis.InvalidateEmployeeAsync(model.EmpId);
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
    }
}