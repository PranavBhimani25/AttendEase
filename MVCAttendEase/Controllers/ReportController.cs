using Microsoft.AspNetCore.Mvc;
using Repositories.Implementation;
using Repositories.Interfaces;

[Route("[controller]")]
public class ReportController : Controller
{
        private readonly IReportRepository _repo;

        public ReportController(IReportRepository repo)
        {
            _repo = repo;
        }
    [Route("EmployeeMonthlyReport")]

    public IActionResult EmployeeMonthlyReport()
    {
          var data = new List<object>()
    {
        new { Date="2026-03-01", ClockIn="09:05", ClockOut="18:10", Late="No", EarlyOut="No", Hours=9 },
        new { Date="2026-03-02", ClockIn="09:20", ClockOut="17:45", Late="Yes", EarlyOut="Yes", Hours=8 },
        new { Date="2026-03-03", ClockIn="08:55", ClockOut="18:30", Late="No", EarlyOut="No", Hours=9.5 }
    };

    return View(data);
    }

    public JsonResult GetReport(int empid,int month,int year)
    {
        var data = _repo.GetMonthlyReport(empid,month,year);

        return Json(data);
    }
}