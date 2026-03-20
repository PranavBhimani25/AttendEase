using Npgsql;
using Repositories.Interfaces;
using Repositories.Models;
using Npgsql;
using System.Data;

namespace Repositories.Implementation
{
    public class AdminRepository : IAdminInterface
    {
        private readonly NpgsqlConnection _conn;

        public AdminRepository(NpgsqlConnection connection)
        {
            _conn = connection;
        }


        public async Task<int> AbsentEmpCount()
        {
            try
            {
                var query=@"SELECT COUNT(*) FROM t_employees e WHERE e.c_status = 'Active' AND e.c_role = 'Employee' AND e.c_empid NOT IN( SELECT c_empid FROM t_attendance WHERE c_attenddate = CURRENT_DATE);";
                using NpgsqlCommand cmd = new NpgsqlCommand(query, _conn);
                await _conn.CloseAsync();
                await _conn.OpenAsync();
                var count=await cmd.ExecuteScalarAsync();

                return Convert.ToInt32(count);
            }catch(Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return -1;
            }
            finally
            {
                await _conn.CloseAsync();
            }
        }

        public async Task<int> CountDesigningHour()
        {
            try
            {
                var query = @"WITH task_hours AS (
                                                SELECT 
                                                    unnest(string_to_array(c_tasktype, ',')) AS task, 
                                                    c_workinghour
                                                FROM t_attendance
                                                WHERE date_trunc('month', c_attenddate) = date_trunc('month', CURRENT_DATE)
                                            )
                                            SELECT 
                                                SUM(c_workinghour) AS total_working_hours
                                            FROM task_hours
                                            GROUP BY task
                                            HAVING task = 'Designing';";
                using NpgsqlCommand cmd = new NpgsqlCommand(query, _conn);
                await _conn.CloseAsync();
                await _conn.OpenAsync();
                var result = await cmd.ExecuteScalarAsync();

                return Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return -1;
            }
            finally
            {
                await _conn.CloseAsync();
            }
        }

        public async Task<int> CountDevelopingHour()
        {
            try
            {
                var query =@"WITH task_hours AS (
                                                SELECT 
                                                    unnest(string_to_array(c_tasktype, ',')) AS task, 
                                                    c_workinghour
                                                FROM t_attendance
                                                WHERE date_trunc('month', c_attenddate) = date_trunc('month', CURRENT_DATE)
                                            )
                                            SELECT 
                                                SUM(c_workinghour) AS total_working_hours
                                            FROM task_hours
                                            GROUP BY task
                                            HAVING task = 'Developing';";
                using NpgsqlCommand cmd = new NpgsqlCommand(query, _conn);
                await _conn.CloseAsync();
                await _conn.OpenAsync();
                var result = await cmd.ExecuteScalarAsync();

                return Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return -1;
            }
            finally
            {
                await _conn.CloseAsync();
            }
        }

        public async Task<int> CountResearchHour()
        {
            try
            {
                var query = @"WITH task_hours AS (
                                                SELECT 
                                                    unnest(string_to_array(c_tasktype, ',')) AS task, 
                                                    c_workinghour
                                                FROM t_attendance
                                                WHERE date_trunc('month', c_attenddate) = date_trunc('month', CURRENT_DATE)
                                            )
                                            SELECT 
                                                SUM(c_workinghour) AS total_working_hours
                                            FROM task_hours
                                            GROUP BY task
                                            HAVING task = 'Research';";
                using NpgsqlCommand cmd = new NpgsqlCommand(query, _conn);
                await _conn.CloseAsync();
                await _conn.OpenAsync();
                var result = await cmd.ExecuteScalarAsync();

                return Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return -1;
            }
            finally
            {
                await _conn.CloseAsync();
            }
        }

        public async Task<int> GetEmployeeCount()
        {
            try
            {
                var query = "SELECT COUNT(*) FROM t_employees where c_role = 'Employee'";
                using NpgsqlCommand cmd = new NpgsqlCommand(query, _conn);
                await _conn.CloseAsync();
                await _conn.OpenAsync();
                var result = await cmd.ExecuteScalarAsync();

                return Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return -1;
            }
            finally
            {
                await _conn.CloseAsync();
            }
        }

        public async Task<List<EmployeeModel>> ListEmployee()
        {   
            var employees = new List<EmployeeModel>();
            try
            {
                var query ="SELECT c_empid, c_name, c_email, c_password, c_gender, c_status, c_profileimage, c_role FROM t_employees where c_role='Employee' order by c_empid;";
                using NpgsqlCommand cmd = new NpgsqlCommand(query, _conn);
                await _conn.CloseAsync();
                await _conn.OpenAsync();
                var reader = await cmd.ExecuteReaderAsync();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        var employee = new EmployeeModel
                        {
                            EmpId=Convert.ToInt32(reader["c_empid"]),
                            Name=Convert.ToString(reader["c_name"])!,
                            Email=Convert.ToString(reader["c_email"])!,
                            Password=Convert.ToString(reader["c_password"])!,
                            Gender=Convert.ToString(reader["c_gender"])!,
                            Status=Convert.ToString(reader["c_status"])!,
                            ProfileImage=Convert.ToString(reader["c_profileimage"])!,
                            Role=Convert.ToString(reader["c_role"])!
                        };
                        employees.Add(employee);
                    }
                }
            }catch(Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return null!;
            }
            finally
            {
                await _conn.CloseAsync();
            }
            return employees;

        }
        public async Task<List<EmployeeReportGridModel>> GetEmployeesForReport()
        {
            var employees = new List<EmployeeReportGridModel>();

            try
            {
                string query = "SELECT c_empid, c_name FROM t_employees WHERE c_role='Employee' order by c_empid;";

                await _conn.OpenAsync();

                using var cmd = new NpgsqlCommand(query, _conn);
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    employees.Add(new EmployeeReportGridModel
                    {
                        EmpId = Convert.ToInt32(reader["c_empid"]),
                        Name = reader["c_name"].ToString()!
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return employees;
            }
            finally
            {
                await _conn.CloseAsync();
            }

            return employees;
        }
            
           

        public async Task<int> OnLeaveEmpCount()
        {
             try
            {
                var query = "SELECT COUNT(*) AS leave_count FROM t_employees WHERE c_status = 'Inactive';";
                
                using NpgsqlCommand cmd = new NpgsqlCommand(query, _conn);
                await _conn.CloseAsync();
                await _conn.OpenAsync();
                var count=await cmd.ExecuteScalarAsync();
                return Convert.ToInt32(count);

            }catch(Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return -1;
            }
            finally
            {
                await _conn.CloseAsync();
            }
        }

        public async Task<int> PresentEmpCount()
        {
            try
            {
                var query = @"SELECT COUNT(*) 
                                    FROM t_employees e
                                    WHERE e.c_status = 'Active'
                                    AND e.c_role = 'Employee'
                                    AND e.c_empid IN (
                                        SELECT c_empid 
                                        FROM t_attendance 
                                        WHERE c_attenddate = CURRENT_DATE
                            );";
                
                using NpgsqlCommand cmd = new NpgsqlCommand(query, _conn);
                await _conn.CloseAsync();
                await _conn.OpenAsync();
                var count=await cmd.ExecuteScalarAsync();
                return Convert.ToInt32(count);

            }catch(Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return -1;
            }
        }

        public async Task<EmployeeModel> GetEmployeeDetails(int empId)
        {
            EmployeeModel emp = new EmployeeModel();

            try
            {
                string query = @"SELECT c_empid,c_name,c_email,c_gender,
                                 c_status,c_profileimage
                                 FROM t_employees
                                 WHERE c_empid=@empid";

                await _conn.OpenAsync();

                using var cmd = new NpgsqlCommand(query, _conn);
                cmd.Parameters.AddWithValue("@empid", empId);

                using var reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    emp.EmpId = Convert.ToInt32(reader["c_empid"]);
                    emp.Name = reader["c_name"].ToString()!;
                    emp.Email = reader["c_email"].ToString()!;
                    emp.Gender = reader["c_gender"].ToString()!;
                    emp.Status = reader["c_status"].ToString()!;
                    emp.ProfileImage = reader["c_profileimage"].ToString()!;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                await _conn.CloseAsync();
            }
            return emp;
        }

        public async Task<int> UpdateEmpStatus(int id,string status)
        {
            try
            {  
                var query = "UPDATE t_employees SET c_status = @status WHERE c_empid = @id";
                using NpgsqlCommand cmd = new NpgsqlCommand(query, _conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@status", status);
                await _conn.CloseAsync();
                await _conn.OpenAsync();
                var result=await cmd.ExecuteNonQueryAsync();
                await _conn.CloseAsync();
                return Convert.ToInt32(result);                
            }catch(Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return -1;
            }
        }

           

        public async Task<MonthlyReportModel> GetEmployeeMonthlyReport(int empId, int month, int year)
        {
            MonthlyReportModel report = new MonthlyReportModel
            {
                EmpId = empId,
                Month = month,
                Year = year
            };

            try
            {
                await _conn.OpenAsync();
                var columns = await GetAttendanceColumnsAsync();
                var workingHourColumn = ResolveAttendanceColumn(columns, "c_workinghour", "c_workinghours", "c_workhour", "c_hours");
                var workingHourExpr = string.IsNullOrWhiteSpace(workingHourColumn)
                    ? "0"
                    : $"COALESCE({workingHourColumn},0)";

                string query = @"
                SELECT
                COUNT(*) FILTER (WHERE c_attendstatus='LateIn') as latein,
                COUNT(*) FILTER (WHERE c_attendstatus='EarlyOut') as earlyout,
                COUNT(*) FILTER (WHERE c_attendstatus='Regular') as regular,
                COUNT(*) as present,
                COALESCE(SUM(" + workingHourExpr + @"),0) as totalworkinghours
                FROM t_attendance
                WHERE c_empid=@empid
                AND EXTRACT(MONTH FROM c_attenddate)=@month
                AND EXTRACT(YEAR FROM c_attenddate)=@year
                AND EXTRACT(ISODOW FROM c_attenddate) BETWEEN 1 AND 5";

                using var cmd = new NpgsqlCommand(query, _conn);

                cmd.Parameters.AddWithValue("@empid", empId);
                cmd.Parameters.AddWithValue("@month", month);
                cmd.Parameters.AddWithValue("@year", year);

                using var reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    report.LateInDays = Convert.ToInt32(reader["latein"]);
                    report.EarlyOutDays = Convert.ToInt32(reader["earlyout"]);
                    report.RegularDays = Convert.ToInt32(reader["regular"]);
                    report.PresentDays = Convert.ToInt32(reader["present"]);
                    report.TotalWorkingHours = Convert.ToDecimal(reader["totalworkinghours"]);
                }

                int workingDays = GetWorkingDaysInMonth(year, month);
                report.AbsentDays = Math.Max(0, workingDays - report.PresentDays);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                await _conn.CloseAsync();
            }

            return report;
        }

        public async Task<AdminMonthlyReportDataModel> GetEmployeeMonthlyReportData(int empId, int month, int year)
        {
            var reportData = new AdminMonthlyReportDataModel
            {
                Summary = await GetEmployeeMonthlyReport(empId, month, year)
            };

            try
            {
                await _conn.OpenAsync();
                var columns = await GetAttendanceColumnsAsync();
                var workingHourColumn = ResolveAttendanceColumn(columns, "c_workinghour", "c_workinghours", "c_workhour", "c_hours");
                var workingHourExpr = string.IsNullOrWhiteSpace(workingHourColumn)
                    ? "0"
                    : $"COALESCE({workingHourColumn},0)";

                string dailyHourQuery = @"
                SELECT EXTRACT(DAY FROM c_attenddate)::int as dayno,
                       COALESCE(SUM(" + workingHourExpr + @"),0) as workinghours
                FROM t_attendance
                WHERE c_empid=@empid
                AND EXTRACT(MONTH FROM c_attenddate)=@month
                AND EXTRACT(YEAR FROM c_attenddate)=@year
                AND EXTRACT(ISODOW FROM c_attenddate) BETWEEN 1 AND 5
                GROUP BY EXTRACT(DAY FROM c_attenddate)
                ORDER BY dayno";

                using var dailyCmd = new NpgsqlCommand(dailyHourQuery, _conn);
                dailyCmd.Parameters.AddWithValue("@empid", empId);
                dailyCmd.Parameters.AddWithValue("@month", month);
                dailyCmd.Parameters.AddWithValue("@year", year);

                using var dailyReader = await dailyCmd.ExecuteReaderAsync();
                while (await dailyReader.ReadAsync())
                {
                    reportData.DailyHours.Add(new DailyWorkingHourModel
                    {
                        Day = Convert.ToInt32(dailyReader["dayno"]),
                        WorkingHours = Convert.ToDecimal(dailyReader["workinghours"])
                    });
                }
                await dailyReader.CloseAsync();

                string yearlyHourQuery = @"
                SELECT EXTRACT(MONTH FROM c_attenddate)::int as monthno,
                       COALESCE(SUM(" + workingHourExpr + @"),0) as workinghours
                FROM t_attendance
                WHERE c_empid=@empid
                AND EXTRACT(YEAR FROM c_attenddate)=@year
                AND EXTRACT(ISODOW FROM c_attenddate) BETWEEN 1 AND 5
                GROUP BY EXTRACT(MONTH FROM c_attenddate)
                ORDER BY monthno";

                var yearHoursByMonth = new Dictionary<int, decimal>();
                using var yearlyCmd = new NpgsqlCommand(yearlyHourQuery, _conn);
                yearlyCmd.Parameters.AddWithValue("@empid", empId);
                yearlyCmd.Parameters.AddWithValue("@year", year);

                using var yearlyReader = await yearlyCmd.ExecuteReaderAsync();
                while (await yearlyReader.ReadAsync())
                {
                    int monthNo = Convert.ToInt32(yearlyReader["monthno"]);
                    decimal hours = Convert.ToDecimal(yearlyReader["workinghours"]);
                    yearHoursByMonth[monthNo] = hours;
                }

                for (int monthNo = 1; monthNo <= 12; monthNo++)
                {
                    reportData.YearlyWorkingHours.Add(new YearlyWorkingHourModel
                    {
                        MonthNo = monthNo,
                        MonthLabel = new DateTime(year, monthNo, 1).ToString("MMM"),
                        WorkingHours = yearHoursByMonth.TryGetValue(monthNo, out var hours) ? hours : 0
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                await _conn.CloseAsync();
            }

            try
            {
                var monthStart = new DateTime(year, month, 1);
                var monthEnd = monthStart.AddMonths(1);
                Console.WriteLine($"EmpId:{empId} Start:{monthStart} End:{monthEnd}");

                await _conn.OpenAsync();

                string detailsQuery = @"
                SELECT
                    a.c_attenddate as attenddate,
                    COALESCE(
                        to_jsonb(a)->>'c_worktype',
                        to_jsonb(a)->>'c_work_type',
                        to_jsonb(a)->>'c_workingtype',
                        ''
                    ) as worktype,
                    COALESCE(
                        to_jsonb(a)->>'c_tasktype',
                        to_jsonb(a)->>'c_task_type',
                        to_jsonb(a)->>'c_task',
                        ''
                    ) as tasktype,
                    COALESCE(
                        NULLIF(to_jsonb(a)->>'c_workinghour','')::numeric,
                        NULLIF(to_jsonb(a)->>'c_workinghours','')::numeric,
                        NULLIF(to_jsonb(a)->>'c_workhour','')::numeric,
                        NULLIF(to_jsonb(a)->>'c_hours','')::numeric,
                        0
                    ) as workinghours,
                    COALESCE(to_jsonb(a)->>'c_attendstatus', '') as attendstatus
                FROM t_attendance a
                WHERE a.c_empid = @empid
                  AND a.c_attenddate >= @monthStart
                  AND a.c_attenddate < @monthEnd
                  AND EXTRACT(ISODOW FROM a.c_attenddate) BETWEEN 1 AND 5
                ORDER BY a.c_attenddate";

                using var detailsCmd = new NpgsqlCommand(detailsQuery, _conn);
                detailsCmd.Parameters.AddWithValue("@empid", empId);
                detailsCmd.Parameters.AddWithValue("@monthStart", monthStart);
                detailsCmd.Parameters.AddWithValue("@monthEnd", monthEnd);

                using var detailsReader = await detailsCmd.ExecuteReaderAsync();
                while (await detailsReader.ReadAsync())
                {
                    var attendDate = detailsReader.GetFieldValue<DateOnly>("attenddate");

                    reportData.AttendanceDetails.Add(new MonthlyAttendanceDetailModel
                    {
                        AttendDate = attendDate.ToDateTime(TimeOnly.MinValue),
                        WorkType = detailsReader["worktype"]?.ToString() ?? "",
                        TaskType = detailsReader["tasktype"]?.ToString() ?? "",
                        WorkingHours = Convert.ToDecimal(detailsReader["workinghours"]),
                        AttendStatus = detailsReader["attendstatus"]?.ToString() ?? ""
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                await _conn.CloseAsync();
            }

            return reportData;
        }

        private async Task<HashSet<string>> GetAttendanceColumnsAsync()
        {
            var cols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            using var cmd = new NpgsqlCommand(@"
                SELECT column_name
                FROM information_schema.columns
                WHERE table_schema='public' AND table_name='t_attendance';", _conn);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                cols.Add(reader["column_name"].ToString() ?? string.Empty);
            }

            return cols;
        }

        private static string ResolveAttendanceColumn(HashSet<string> existingColumns, params string[] candidates)
        {
            foreach (var c in candidates)
            {
                if (existingColumns.Contains(c))
                {
                    return c;
                }
            }

            return string.Empty;
        }

        private static int GetWorkingDaysInMonth(int year, int month)
        {
            int daysInMonth = DateTime.DaysInMonth(year, month);
            int workingDays = 0;

            for (int day = 1; day <= daysInMonth; day++)
            {
                var currentDate = new DateTime(year, month, day);
                if (currentDate.DayOfWeek != DayOfWeek.Saturday && currentDate.DayOfWeek != DayOfWeek.Sunday)
                {
                    workingDays++;
                }
            }

            return workingDays;
        }


        public async Task<EmployeeModel> GetEmployeeById(int empId)
        {
            var qry = "SELECT c_email FROM t_employees WHERE c_empid=@id";
            try
            {
                using (NpgsqlCommand cmd = new NpgsqlCommand(qry, _conn))
                {

                    cmd.Parameters.AddWithValue("@id", empId);
                    await _conn.CloseAsync();
                    await _conn.OpenAsync();
                    var reader = await cmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        return new EmployeeModel
                        {
                            Email = reader["c_email"].ToString()
                        };
                    }
                }

                return null;

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error is: " + ex.Message);
                return null;
            }
            finally
            {
                await _conn.CloseAsync();
            }

        }
    }
}
