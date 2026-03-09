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
    [ServiceFilter(typeof(EmployeeFilter))]
    public class EmployeeController : Controller
    {
        private readonly IEmployeeInterface _repo;
        private readonly ILogger<EmployeeController> _logger;

        public EmployeeController(ILogger<EmployeeController> logger, IEmployeeInterface repo)
        {
            _logger = logger;
            _repo = repo;
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

        [HttpGet("Report")]
        public IActionResult Report()
        {
            int empId = GetCurrentEmpId();
            if (empId <= 0)
            {
                return RedirectToAction("Login", "Auth");
            }

            var dashboard = _repo.GetReportData(empId);

            return View(dashboard);
        }

        [HttpGet("[action]")]
        public JsonResult GetAttendance(int empId)
        {
            if (empId <= 0) empId = GetCurrentEmpId();
            var data = _repo.GetAttendanceByEmployee(empId);

            return Json(data);
        }

        [HttpGet("[action]")]
        public JsonResult GetReportYearData(int empId, int year)
        {
            if (empId <= 0) empId = GetCurrentEmpId();
            var data = _repo.GetReportYearData(empId, year);

            return Json(data);
        }

        [HttpGet("[action]")]
        public JsonResult GetAttendanceYears(int empId)
        {
            if (empId <= 0) empId = GetCurrentEmpId();
            var years = _repo.GetAttendanceYears(empId);

            return Json(years);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}
