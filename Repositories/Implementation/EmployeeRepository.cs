using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using Repositories.Interfaces;
using Repositories.Models;

namespace Repositories.Implementation
{
    public class EmployeeRepository : IEmployeeInterface
    {
        private readonly NpgsqlConnection _conn;

        public EmployeeRepository(NpgsqlConnection conn)
        {
            _conn = conn;
        }
        public async Task<EmployeeModel> getEmployeeDetails(int id)
        {
            try
            {
                var query = "SELECT c_empid, c_name, c_email, c_gender, c_profileimage from t_employees where c_empid = @empid";
                await _conn.OpenAsync();
                using var command = new NpgsqlCommand(query, _conn);
                command.Parameters.AddWithValue("@empid", id);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new EmployeeModel
                    {
                        EmpId = reader.GetInt32(reader.GetOrdinal("c_empid")),
                        Name = reader.GetString(reader.GetOrdinal("c_name")),
                        Email = reader.GetString(reader.GetOrdinal("c_email")),
                        Gender = reader.GetString(reader.GetOrdinal("c_gender")),
                        ProfileImage = reader.GetString(reader.GetOrdinal("c_profileimage")),
                    };
                }
                else
                {
                    return null;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error during Profile {e.Message}");
                return null;
            }
            finally
            {
                await _conn.CloseAsync();
            }

        }



        public async Task<EmployeeModel> GetOne(int id)
        {
            var qry = "SELECT * FROM t_employees WHERE c_empid=@id";
            EmployeeModel emp = new EmployeeModel();
            try
            {
                NpgsqlCommand cmd = new NpgsqlCommand(qry, _conn);
                cmd.Parameters.AddWithValue("@id", id);
                await _conn.OpenAsync();
                var reader = await cmd.ExecuteReaderAsync();
                if (reader.Read())
                {
                    emp.EmpId = (int)reader["c_empid"];
                    emp.Name = (string)reader["c_name"];
                    emp.Email = (string)reader["c_email"];
                    emp.Gender = (string)reader["c_gender"];
                    emp.Role = (string)reader["c_role"];
                    emp.Status = (string)reader["c_status"];
                    emp.ProfileImage = (string)reader["c_profileimage"];
                }
                await _conn.CloseAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error is: " + ex.Message);
            }
            finally
            {
                await _conn.CloseAsync();
            }
            return emp;
        }



        public async Task<int> ChangePWD(int id, string pass)
        {
            var qry = "UPDATE t_employees SET c_password=@pass WHERE c_empid=@id";
            try
            {
                NpgsqlCommand cmd = new NpgsqlCommand(qry, _conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@pass", pass);
                await _conn.OpenAsync();
                var status = await cmd.ExecuteNonQueryAsync();
                if (status == 1)
                {
                    Console.WriteLine("Password Chnage SuccessFully...");
                }
                await _conn.CloseAsync();
                return 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error is: " + ex.Message);
                return 0;
            }
            finally
            {
                await _conn.CloseAsync();
            }
        }



        public async Task<int> Update(UpdateEmployee emp)
        {
            var qry = "UPDATE t_employees SET c_name=@name,c_email=@email,c_gender=@gender,c_profileimage=@image WHERE c_empid=@id";
            try
            {
                NpgsqlCommand cmd = new NpgsqlCommand(qry, _conn);
                cmd.Parameters.AddWithValue("@name", emp.Name);
                cmd.Parameters.AddWithValue("@email", emp.Email);
                cmd.Parameters.AddWithValue("@gender", emp.Gender);
                cmd.Parameters.AddWithValue("@image", emp.ProfileImageUrl);
                cmd.Parameters.AddWithValue("@id", emp.EmpId);

                await _conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
                await _conn.CloseAsync();
                return 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error is: " + ex.Message);
                return 0;
            }
            finally
            {
                await _conn.CloseAsync();
            }
        }




        public async Task<List<vm_AttendenceUser>> GetAttendanceByEmployee(int empId, int year)
        {
            List<vm_AttendenceUser> list = new List<vm_AttendenceUser>();

            var qry = @"SELECT c_attendid,c_empid,c_attenddate FROM t_attendance WHERE c_empid=@empid AND EXTRACT(YEAR FROM c_attenddate)=@year";
            try
            {
                NpgsqlCommand cmd = new NpgsqlCommand(qry, _conn);
                cmd.Parameters.AddWithValue("@empid", empId);
                // cmd.Parameters.AddWithValue("@month",month);
                cmd.Parameters.AddWithValue("@year", year);

                await _conn.OpenAsync();

                var reader = await cmd.ExecuteReaderAsync();

                while (reader.Read())
                {
                    vm_AttendenceUser att = new vm_AttendenceUser();

                    att.AttendID = (int)reader["c_attendid"];
                    att.EmpID = (int)reader["c_empid"];
                    att.AttendDate = (DateOnly)reader["c_attenddate"];

                    att.Status = "Present";

                    list.Add(att);
                }
                await _conn.CloseAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error : " + ex.Message);
            }
            finally
            {
                await _conn.CloseAsync();
            }
            return list;
        }



        public async Task<List<vm_YearlyWorkingHours>> GetYearlyWorkingHours(int empId, int year)
        {
            List<vm_YearlyWorkingHours> list = new List<vm_YearlyWorkingHours>();

            var qry = @"SELECT 
                        TO_CHAR(c_attenddate,'Mon') AS monthname,
                        EXTRACT(MONTH FROM c_attenddate) AS month,
                        SUM(c_workinghour) AS totalhours
                    FROM t_attendance
                    WHERE c_empid=@empid
                    AND EXTRACT(YEAR FROM c_attenddate)=@year
                    GROUP BY monthname,EXTRACT(MONTH FROM c_attenddate)
                    ORDER BY EXTRACT(MONTH FROM c_attenddate)";

            try
            {
                NpgsqlCommand cmd = new NpgsqlCommand(qry, _conn);

                cmd.Parameters.AddWithValue("@empid", empId);
                cmd.Parameters.AddWithValue("@year", year);

                await _conn.OpenAsync();

                var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    vm_YearlyWorkingHours obj = new vm_YearlyWorkingHours();

                    obj.Month = Convert.ToInt32(reader["month"]);
                    obj.MonthName = reader["monthname"].ToString().Trim();
                    obj.TotalHours = Convert.ToInt32(reader["totalhours"]);

                    list.Add(obj);
                }

                await reader.CloseAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                await _conn.CloseAsync();
            }

            return list;
        }



        public async Task<List<vm_MonthlyWorkingHours>> GetMonthlyWorkingHours(int empId, int month, int year)
        {
            List<vm_MonthlyWorkingHours> list = new List<vm_MonthlyWorkingHours>();

            var qry = @"SELECT 
                    c_attenddate,
                    c_workinghour
                FROM t_attendance
                WHERE c_empid = @empid
                AND EXTRACT(MONTH FROM c_attenddate) = @month
                AND EXTRACT(YEAR FROM c_attenddate) = @year
                ORDER BY c_attenddate";

            try
            {
                NpgsqlCommand cmd = new NpgsqlCommand(qry, _conn);

                cmd.Parameters.AddWithValue("@empid", empId);
                cmd.Parameters.AddWithValue("@month", month);
                cmd.Parameters.AddWithValue("@year", year);

                await _conn.OpenAsync();

                var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    vm_MonthlyWorkingHours obj = new vm_MonthlyWorkingHours();

                    obj.Date = (DateOnly)reader["c_attenddate"];
                    obj.WorkingHour = Convert.ToInt32(reader["c_workinghour"]);

                    list.Add(obj);
                }

                await _conn.CloseAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error : " + ex.Message);
            }
            finally
            {
                await _conn.CloseAsync();
            }

            return list;
        }

        private static bool IsWeekday(DateTime date) =>
            date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday;

        private static int CountWeekdays(DateOnly start, DateOnly end)
        {
            if (end < start)
            {
                return 0;
            }

            int count = 0;
            DateTime cursor = start.ToDateTime(TimeOnly.MinValue);
            DateTime last = end.ToDateTime(TimeOnly.MinValue);

            while (cursor <= last)
            {
                if (IsWeekday(cursor))
                {
                    count++;
                }

                cursor = cursor.AddDays(1);
            }

            return count;
        }

        /* ================= EMPLOYEE DETAILS ================= */

        public EmployeeModel GetEmployee(int empId)
        {
            EmployeeModel emp = new EmployeeModel();


            _conn.Open();

            string query = "SELECT * FROM t_employees WHERE c_empid=@id";

            using (var cmd = new NpgsqlCommand(query, _conn))
            {
                cmd.Parameters.AddWithValue("@id", empId);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        emp.EmpId = Convert.ToInt32(reader["c_empid"]);
                        emp.Name = reader["c_name"].ToString();
                        emp.Email = reader["c_email"].ToString();
                        emp.Gender = reader["c_gender"].ToString();
                        emp.Role = reader["c_role"].ToString();
                        emp.ProfileImage = reader["c_profileimage"].ToString();
                        emp.Status = reader["c_status"].ToString();
                    }
                }
            }


            return emp;
        }

        /* ================= GRID DATA ================= */

        public List<AttendanceModel> GetAttendanceByEmployee(int empId)
        {
            List<AttendanceModel> list = new List<AttendanceModel>();

            // using (var conn = new NpgsqlConnection(_conn))
            // {
            if (_conn.State != System.Data.ConnectionState.Open)
                    _conn.Open();


            string query = @"SELECT * FROM t_attendance
                                 WHERE c_empid=@id
                                 ORDER BY c_attenddate DESC";

            using (var cmd = new NpgsqlCommand(query, _conn))
            {
                if (_conn.State != System.Data.ConnectionState.Open)
                    _conn.Open(); // i do change here
                cmd.Parameters.AddWithValue("@id", empId);

                using (var reader = cmd.ExecuteReader())
                {
                    int attendIdOrdinal = reader.GetOrdinal("c_attendid");
                    int empIdOrdinal = reader.GetOrdinal("c_empid");
                    int attendDateOrdinal = reader.GetOrdinal("c_attenddate");
                    int clockInHourOrdinal = reader.GetOrdinal("c_clockinhour");
                    int clockInMinOrdinal = reader.GetOrdinal("c_clockinmin");
                    int clockOutHourOrdinal = reader.GetOrdinal("c_clockouthour");
                    int clockOutMinOrdinal = reader.GetOrdinal("c_clockoutmin");
                    int workingHourOrdinal = reader.GetOrdinal("c_workinghour");
                    int attendStatusOrdinal = reader.GetOrdinal("c_attendstatus");
                    int workTypeOrdinal = reader.GetOrdinal("c_worktype");
                    int taskTypeOrdinal = reader.GetOrdinal("c_tasktype");

                    while (reader.Read())
                    {
                        var attendDateValue = reader.GetValue(attendDateOrdinal);
                        DateTime attendDate = attendDateValue switch
                        {
                            DateOnly d => d.ToDateTime(TimeOnly.MinValue),
                            DateTime dt => dt,
                            _ => Convert.ToDateTime(attendDateValue)
                        };

                        list.Add(new AttendanceModel
                        {
                            AttendId = reader.GetInt32(attendIdOrdinal),
                            EmpId = reader.GetInt32(empIdOrdinal),
                            AttendDate = attendDate,
                            ClockInHour = reader.GetInt32(clockInHourOrdinal),
                            ClockInMin = reader.GetInt32(clockInMinOrdinal),
                            ClockOutHour = reader.IsDBNull(clockOutHourOrdinal) ? null : reader.GetInt32(clockOutHourOrdinal),
                            ClockOutMin = reader.IsDBNull(clockOutMinOrdinal) ? null : reader.GetInt32(clockOutMinOrdinal),
                            WorkingHour = reader.IsDBNull(workingHourOrdinal) ? null : reader.GetInt32(workingHourOrdinal),
                            AttendStatus = reader.IsDBNull(attendStatusOrdinal) ? string.Empty : reader.GetString(attendStatusOrdinal),
                            WorkType = reader.IsDBNull(workTypeOrdinal) ? string.Empty : reader.GetString(workTypeOrdinal),
                            TaskType = reader.IsDBNull(taskTypeOrdinal) ? null : reader.GetString(taskTypeOrdinal)
                        });
                    }
                }            
                
            }


            return list;
        }

        /* ================= DASHBOARD YEAR DATA ================= */

        public ReportYearModel GetReportYearData(int empId, int year)
        {
            ReportYearModel model = new ReportYearModel();

            model.PresentMonthly = new List<int>(new int[12]);
            model.AbsentMonthly = new List<int>(new int[12]);



            model.Regular = new List<int>(new int[12]);
            model.LateIn = new List<int>(new int[12]);
            model.EarlyOut = new List<int>(new int[12]);

            var today = DateTime.Today;
            var startDate = new DateOnly(year, 1, 1);
            var endDate = year < today.Year
                ? new DateOnly(year, 12, 31)
                : year == today.Year
                    ? DateOnly.FromDateTime(today)
                    : new DateOnly(year, 12, 31);
            for (int i = 0; i < 12; i++)
            {
                int monthNumber = i + 1;
                int daysToConsider = 0;

                if (year < today.Year)
                {
                    var monthStart = new DateOnly(year, monthNumber, 1);
                    var monthEnd = new DateOnly(year, monthNumber, DateTime.DaysInMonth(year, monthNumber));
                    daysToConsider = CountWeekdays(monthStart, monthEnd);
                }
                else if (year == today.Year)
                {
                    if (monthNumber < today.Month)
                    {
                        var monthStart = new DateOnly(year, monthNumber, 1);
                        var monthEnd = new DateOnly(year, monthNumber, DateTime.DaysInMonth(year, monthNumber));
                        daysToConsider = CountWeekdays(monthStart, monthEnd);
                    }
                    else if (monthNumber == today.Month)
                    {
                        var monthStart = new DateOnly(year, monthNumber, 1);
                        var monthEnd = DateOnly.FromDateTime(today);
                        daysToConsider = CountWeekdays(monthStart, monthEnd);
                    }
                }

                model.AbsentMonthly[i] = daysToConsider;
            }

            // using (var conn = new NpgsqlConnection(_conn))
            // {
            _conn.Open();

            /* ================= SUMMARY ================= */

            string summaryQuery = @"SELECT
                                        COUNT(DISTINCT CASE 
                                            WHEN EXTRACT(ISODOW FROM c_attenddate) < 6 
                                            THEN c_attenddate 
                                        END) AS present,
                                        COALESCE(SUM(c_workinghour),0) AS hours
                                        FROM t_attendance
                                        WHERE c_empid=@empId 
                                        AND c_attenddate BETWEEN @startDate AND @endDate";

            using (var cmd = new NpgsqlCommand(summaryQuery, _conn))
            {
                cmd.Parameters.AddWithValue("@empId", empId);
                cmd.Parameters.AddWithValue("@startDate", startDate);
                cmd.Parameters.AddWithValue("@endDate", endDate);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        model.Present = Convert.ToInt32(reader["present"]);
                        model.TotalHours = Convert.ToInt32(reader["hours"]);
                    }
                }
            }

            int totalDaysToConsider = CountWeekdays(startDate, endDate);

            model.Absent = Math.Max(0, totalDaysToConsider - model.Present);

            /* ================= WORK TYPE PIE ================= */

            string workQuery = @"SELECT
                                     SUM(CASE WHEN LOWER(TRIM(c_worktype))='remote' THEN 1 ELSE 0 END) AS remote,
                                     SUM(CASE WHEN LOWER(TRIM(c_worktype))='office' THEN 1 ELSE 0 END) AS office,
                                     SUM(CASE WHEN LOWER(TRIM(c_worktype))='field' THEN 1 ELSE 0 END) AS field
                                     FROM t_attendance
                                     WHERE c_empid=@empId
                                     AND c_attenddate BETWEEN @startDate AND @endDate";

            using (var cmd = new NpgsqlCommand(workQuery, _conn))
            {
                cmd.Parameters.AddWithValue("@empId", empId);
                cmd.Parameters.AddWithValue("@startDate", startDate);
                cmd.Parameters.AddWithValue("@endDate", endDate);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        model.Remote = Convert.ToInt32(reader["remote"]);
                        model.Office = Convert.ToInt32(reader["office"]);
                        model.Field = Convert.ToInt32(reader["field"]);
                    }
                }
            }

            /* ================= ATTENDANCE TREND ================= */

            string trendQuery = @"SELECT
                                        EXTRACT(MONTH FROM c_attenddate) AS month,
                                        COUNT(DISTINCT CASE 
                                            WHEN EXTRACT(ISODOW FROM c_attenddate) < 6 
                                           THEN c_attenddate 
                                        END) AS present
                                    FROM t_attendance
                                    WHERE c_empid=@empId
                                    AND c_attenddate BETWEEN @startDate AND @endDate
                                    GROUP BY month
                                    ORDER BY month";

            using (var cmd = new NpgsqlCommand(trendQuery, _conn))
            {
                cmd.Parameters.AddWithValue("@empId", empId);
                cmd.Parameters.AddWithValue("@startDate", startDate);
                cmd.Parameters.AddWithValue("@endDate", endDate);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int month = Convert.ToInt32(reader["month"]) - 1;
                        int present = Convert.ToInt32(reader["present"]);

                        model.PresentMonthly[month] = present;
                        int daysToConsiderInMonth = model.AbsentMonthly[month];
                        model.AbsentMonthly[month] = Math.Max(0, daysToConsiderInMonth - present);
                    }
                }
            }

            /* ================= TASK DISTRIBUTION ================= */

            string taskQuery = @"SELECT
                                    month,
                                
                                    ROUND(SUM(CASE WHEN task='developing' THEN 1 ELSE 0 END)*100.0/COUNT(*),2) AS developing_percentage,
                                    ROUND(SUM(CASE WHEN task='designing' THEN 1 ELSE 0 END)*100.0/COUNT(*),2) AS designing_percentage,
                                    ROUND(SUM(CASE WHEN task IN ('research','testing') THEN 1 ELSE 0 END)*100.0/COUNT(*),2) AS research_percentage
                                
                                FROM (
                                    SELECT 
                                        EXTRACT(MONTH FROM c_attenddate) AS month,
                                        LOWER(TRIM(unnest(string_to_array(c_tasktype, ',')))) AS task
                                    FROM t_attendance
                                    WHERE c_empid = @empId
                                    AND c_attenddate BETWEEN @startDate AND @endDate
                                ) t
                                GROUP BY month
                                ORDER BY month;";

            using (var cmd = new NpgsqlCommand(taskQuery, _conn))
            {
                cmd.Parameters.AddWithValue("@empId", empId);
                cmd.Parameters.AddWithValue("@startDate", startDate);
                cmd.Parameters.AddWithValue("@endDate", endDate);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int month = Convert.ToInt32(reader["month"]) - 1;

                        model.Developing[month] = Convert.ToDouble(reader["developing_percentage"]);
                        model.Designing[month] = Convert.ToDouble(reader["designing_percentage"]);
                        model.Research[month] = Convert.ToDouble(reader["research_percentage"]);
                    }
                }
            }

            /* ================= ATTENDANCE STATUS ================= */

            string statusQuery = @"SELECT
                    EXTRACT(MONTH FROM c_attenddate) AS month,
                    SUM(CASE WHEN LOWER(TRIM(c_attendstatus))='regular' THEN 1 ELSE 0 END) AS regular,
                    SUM(CASE WHEN LOWER(TRIM(c_attendstatus))='latein' THEN 1 ELSE 0 END) AS latein,
                    SUM(CASE WHEN LOWER(TRIM(c_attendstatus))='earlyout' THEN 1 ELSE 0 END) AS earlyout
                    FROM t_attendance
                    WHERE c_empid=@empId
                    AND c_attenddate BETWEEN @startDate AND @endDate
                    GROUP BY month
                    ORDER BY month";

            using (var cmd = new NpgsqlCommand(statusQuery, _conn))
            {
                cmd.Parameters.AddWithValue("@empId", empId);
                cmd.Parameters.AddWithValue("@startDate", startDate);
                cmd.Parameters.AddWithValue("@endDate", endDate);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int month = Convert.ToInt32(reader["month"]) - 1;

                        model.Regular[month] = Convert.ToInt32(reader["regular"]);
                        model.LateIn[month] = Convert.ToInt32(reader["latein"]);
                        model.EarlyOut[month] = Convert.ToInt32(reader["earlyout"]);
                    }
                }
            }


            model.GridData = GetAttendanceByEmployee(empId);

            return model;
        }

        /* ================= MAIN DASHBOARD ================= */

        public ReportdModel GetReportData(int empId)
        {
            ReportdModel dashboard = new ReportdModel();

            dashboard.Employee = GetEmployee(empId);
            dashboard.AttendanceList = GetAttendanceByEmployee(empId);
            dashboard.TotalPresent = dashboard.AttendanceList
                           .Where(x =>
                               x.AttendDate.Value.Year == DateTime.Today.Year &&
                               x.AttendDate.Value.Month == DateTime.Today.Month &&
                               IsWeekday(x.AttendDate.Value))
                           .Select(x => x.AttendDate.Value.Date)
                           .Distinct()
                           .Count();
            dashboard.TotalHours = dashboard.AttendanceList.Sum(x => x.WorkingHour ?? 0);
            var today = DateTime.Today;
            var monthStart = new DateOnly(today.Year, today.Month, 1);
            var monthEnd = DateOnly.FromDateTime(today);
            int presentWeekdaysThisMonth = dashboard.AttendanceList.Where(x =>
                x.AttendDate.Value.Year == today.Year &&
                x.AttendDate.Value.Month == today.Month &&
                IsWeekday(x.AttendDate.Value))
                .Select(x => x.AttendDate.Value.Date)
                .Distinct()
                .Count();
            dashboard.TotalAbsent = Math.Max(0, CountWeekdays(monthStart, monthEnd) - presentWeekdaysThisMonth);

            return dashboard;
        }

        /* ================= YEARS ================= */

        public List<int> GetAttendanceYears(int empId)
        {
            List<int> years = new List<int>();

            // using (var conn = new NpgsqlConnection(_conn))
            // {
            _conn.Open();

            string query = @"SELECT DISTINCT EXTRACT(YEAR FROM c_attenddate) AS year
                                 FROM t_attendance
                                 WHERE c_empid=@empId
                                 ORDER BY year DESC";

            using (var cmd = new NpgsqlCommand(query, _conn))
            {
                cmd.Parameters.AddWithValue("@empId", empId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        years.Add(Convert.ToInt32(reader["year"]));
                    }
                }
            }


            return years;
        }
    }
}
