using System;
using System.Threading.Tasks;
using Npgsql;
using Repositories.Interfaces;
using Repositories.Models;

namespace Repositories.Implementation
{
    public class AttendanceRepository : IAttendanceInterface
    {
        private readonly NpgsqlConnection _conn;

        public AttendanceRepository(NpgsqlConnection conn)
        {
            _conn = conn;
        }

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
            finally
            {
                await _conn.CloseAsync();
            }
        }

        // ================= CHECK IN =================

        public async Task<(bool success, string message, string status)> CheckIn(AttendanceModel model)
        {
            try
            {
                bool exists = await IsTodayAttendanceExists(model.EmpId);

                if (exists)
                    return (false, "Already checked in today", "");

                await _conn.OpenAsync();

                DateTime now = DateTime.Now;
                DateTime officeStart = DateTime.Today.AddHours(9).AddMinutes(15);

                string status = now > officeStart ? "LateIn" : "Regular";

                string qry = @"INSERT INTO t_attendance
                (c_empid,c_attenddate,c_clockinhour,c_clockinmin,c_worktype,c_tasktype,c_attendstatus)
                VALUES
                (@empid,CURRENT_DATE,@hour,@min,@worktype,@tasktype,@status)";

                using var cmd = new NpgsqlCommand(qry, _conn);

                cmd.Parameters.AddWithValue("@empid", model.EmpId);
                cmd.Parameters.AddWithValue("@hour", now.Hour);
                cmd.Parameters.AddWithValue("@min", now.Minute);
                cmd.Parameters.AddWithValue("@worktype", model.WorkType);
                cmd.Parameters.AddWithValue("@tasktype", model.TaskType ?? "");
                cmd.Parameters.AddWithValue("@status", status);

                await cmd.ExecuteNonQueryAsync();

                return (true, "Checked In", status);
            }
            finally
            {
                await _conn.CloseAsync();
            }
        }

        // ================= CHECK OUT =================

        public async Task<(bool success, string message, int workingHours, string status)> CheckOut(AttendanceModel model)
        {
            try
            {
                bool exists = await IsTodayAttendanceExists(model.EmpId);

                if (!exists)
                    return (false, "Please check in first", 0, "");

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
                    cmd.Parameters.AddWithValue("@empid", model.EmpId);

                    using var reader = await cmd.ExecuteReaderAsync();

                    if (await reader.ReadAsync())
                    {
                        checkinHour = reader.GetInt32(0);
                        checkinMin = reader.GetInt32(1);
                        status = reader.GetString(2);

                        if (!reader.IsDBNull(3))
                            checkoutHour = reader.GetInt32(3);
                    }
                }

                if (checkoutHour != null)
                    return (false, "Already checked out today", 0, "");

                DateTime checkinTime = DateTime.Today.AddHours(checkinHour).AddMinutes(checkinMin);

                TimeSpan working = now - checkinTime;

                int workingHours = (int)Math.Round(working.TotalHours, 2);

                DateTime officeEnd = DateTime.Today.AddHours(17);

                bool earlyLeave = now < officeEnd;

                if (status == "LateIn" && earlyLeave)
                {
                    status = "EarlyOut";
                }
                else if (earlyLeave)
                {
                    status = "EarlyOut";
                }

                string updateQry = @"UPDATE t_attendance
                                     SET c_clockouthour=@hour,
                                         c_clockoutmin=@min,
                                         c_workinghour=@working,
                                         c_tasktype=@tasktype,
                                         c_attendstatus=@status
                                     WHERE c_empid=@empid
                                     AND c_attenddate=CURRENT_DATE";

                using var updateCmd = new NpgsqlCommand(updateQry, _conn);

                updateCmd.Parameters.AddWithValue("@hour", now.Hour);
                updateCmd.Parameters.AddWithValue("@min", now.Minute);
                updateCmd.Parameters.AddWithValue("@working", workingHours);
                updateCmd.Parameters.AddWithValue("@tasktype", model.TaskType ?? "");
                updateCmd.Parameters.AddWithValue("@status", status);
                updateCmd.Parameters.AddWithValue("@empid", model.EmpId);

                await updateCmd.ExecuteNonQueryAsync();

                return (true, "Checked Out", workingHours, status);
            }
            finally
            {
                await _conn.CloseAsync();
            }
        }

        public async Task<List<AttendanceModel>> GetAttendanceByEmployee(int empId)
        {
            List<AttendanceModel> list = new();

            await _conn.OpenAsync();

            string query = @"SELECT *
                     FROM t_attendance
                     WHERE c_empid=@empId
                     ORDER BY c_attenddate DESC";

            using var cmd = new NpgsqlCommand(query, _conn);

            cmd.Parameters.AddWithValue("@empId", empId);

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new AttendanceModel
                {

                    AttendId = reader.GetInt32(0),
                    EmpId = reader.GetInt32(1),
                    AttendDate = reader.GetDateTime(2),
                    ClockInHour = reader.GetInt32(3),
                    ClockInMin = reader.GetInt32(4),
                    ClockOutHour = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                    ClockOutMin = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                    WorkingHour = reader.IsDBNull(7) ? null : reader.GetInt32(7),
                    AttendStatus = reader.GetString(8),
                    WorkType = reader.GetString(9),
                    TaskType = reader.IsDBNull(10) ? null : reader.GetString(10)

                });
            }
                return list;
        }
    }
}