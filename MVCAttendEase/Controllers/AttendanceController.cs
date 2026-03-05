using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace MVCAttendEase.Controllers
{
    [Route("[controller]")]
    public class AttendanceController : Controller
    {
        private readonly ILogger<AttendanceController> _logger;
        private readonly NpgsqlConnection _conn;

        public AttendanceController(ILogger<AttendanceController> logger, NpgsqlConnection conn)
        {
            _logger = logger;
            _conn = conn;
        }

        public IActionResult Index()
        {
            return View();
        }

        // Check if today's attendance exists
        public async Task<bool> IsTodayAttendanceExists(int empid)
        {
            try
            {
                await _conn.OpenAsync();

                string qry = @"SELECT COUNT(*)
                               FROM t_attendance
                               WHERE c_empid=@empid
                               AND c_attenddate=CURRENT_DATE";

                using var cmd = new NpgsqlCommand(qry, _conn);
                cmd.Parameters.AddWithValue("@empid", empid);

                int count = Convert.ToInt32(await cmd.ExecuteScalarAsync());

                return count > 0;
            }
            catch (Exception ex)
            {
                return false;
            }
            finally
            {
                await _conn.CloseAsync();
            }
        }

        //  CHECK IN 
        [HttpPost]
        public async Task<IActionResult> CheckIn(int empid, string worktype, string tasktype)
        {
            try
            {
                bool exists = await IsTodayAttendanceExists(empid);

                if (exists)
                {
                    return Json(new { success = false, message = "Already checked in today" });
                }

                await _conn.OpenAsync();

                DateTime now = DateTime.Now;
                DateTime officeStart = DateTime.Today.AddHours(9).AddMinutes(15);

                string status = now > officeStart ? "Late" : "Regular";

                string qry = @"INSERT INTO t_attendance
                (c_empid,c_attenddate,c_clockinhour,c_clockinmin,c_worktype,c_tasktype,c_attendstatus)
                VALUES
                (@empid,CURRENT_DATE,@hour,@min,@worktype,@tasktype,@status)";

                using var cmd = new NpgsqlCommand(qry, _conn);

                cmd.Parameters.AddWithValue("@empid", empid);
                cmd.Parameters.AddWithValue("@hour", now.Hour);
                cmd.Parameters.AddWithValue("@min", now.Minute);
                cmd.Parameters.AddWithValue("@worktype", worktype);
                cmd.Parameters.AddWithValue("@tasktype", tasktype);
                cmd.Parameters.AddWithValue("@status", status);

                await cmd.ExecuteNonQueryAsync();

                return Json(new
                {
                    success = true,
                    message = "Checked In",
                    status = status
                });
            }
            catch (Exception ex)
            {

                return Json(new
                {
                    success = false,
                    message = "Something went wrong"
                });
            }
            finally
            {
                await _conn.CloseAsync();
            }
        }

        //  CHECK OUT 
        [HttpPost]
        public async Task<IActionResult> CheckOut(int empid)
        {
            try
            {
                bool exists = await IsTodayAttendanceExists(empid);

                if (!exists)
                {
                    return Json(new { success = false, message = "Please check in first" });
                }

                await _conn.OpenAsync();

                DateTime now = DateTime.Now;

                int checkinHour = 0;
                int checkinMin = 0;
                int? checkoutHour = null;

                string status = "Regular";

                string fetchQry = @"SELECT c_clockinhour,
                                           c_clockinmin,
                                           c_attendstatus,
                                           c_clockouthour
                                    FROM t_attendance
                                    WHERE c_empid=@empid
                                    AND c_attenddate=CURRENT_DATE";

                using (var cmd = new NpgsqlCommand(fetchQry, _conn))
                {
                    cmd.Parameters.AddWithValue("@empid", empid);

                    using var reader = await cmd.ExecuteReaderAsync();

                    if (await reader.ReadAsync())
                    {
                        checkinHour = reader.GetInt32(0);
                        checkinMin = reader.GetInt32(1);
                        status = reader.GetString(2);

                        if (!reader.IsDBNull(3))
                        {
                            checkoutHour = reader.GetInt32(3);
                        }
                    }
                }

                // prevent double checkout
                if (checkoutHour != null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Already checked out today"
                    });
                }

                DateTime checkinTime =
                    DateTime.Today.AddHours(checkinHour).AddMinutes(checkinMin);

                TimeSpan working = now - checkinTime;

                int workingHours = (int)Math.Round(working.TotalHours, 2);

                DateTime officeEnd = DateTime.Today.AddHours(17);

                bool earlyLeave = now < officeEnd;

                if (status == "Late" && earlyLeave)
                {
                    status = "Late + EarlyLeave";
                }
                else if (earlyLeave)
                {
                    status = "EarlyLeave";
                }

                string updateQry = @"UPDATE t_attendance
                                     SET c_clockouthour=@hour,
                                         c_clockoutmin=@min,
                                         c_workinghour=@working,
                                         c_attendstatus=@status
                                     WHERE c_empid=@empid
                                     AND c_attenddate=CURRENT_DATE";

                using var updateCmd = new NpgsqlCommand(updateQry, _conn);

                updateCmd.Parameters.AddWithValue("@hour", now.Hour);
                updateCmd.Parameters.AddWithValue("@min", now.Minute);
                updateCmd.Parameters.AddWithValue("@working", workingHours);
                updateCmd.Parameters.AddWithValue("@status", status);
                updateCmd.Parameters.AddWithValue("@empid", empid);

                await updateCmd.ExecuteNonQueryAsync();

                return Json(new
                {
                    success = true,
                    message = "Checked Out",
                    workingHours = workingHours,
                    status = status
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Something went wrong"
                });
            }
            finally
            {
                await _conn.CloseAsync();
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error");
        }
    }
}