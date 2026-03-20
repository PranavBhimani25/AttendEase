using Microsoft.Extensions.Configuration;
using Npgsql;
using Repositories.Interfaces;
using Repositories.Models;
using System.Data;

namespace Repositories.Implementation
{
    public class ReportRepository : IReportRepository
    {
        private readonly string _connectionString;

        public ReportRepository(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
        }

        public List<EmployeeReportItem> GetEmployeesForReport()
        {
            var list = new List<EmployeeReportItem>();

            using var con = new NpgsqlConnection(_connectionString);
            const string query = @"SELECT c_empid, c_name, c_email, c_gender, c_status, c_role, c_profileimage
                                   FROM t_employees
                                   ORDER BY c_empid";

            using var cmd = new NpgsqlCommand(query, con);
            con.Open();
            using var dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                list.Add(new EmployeeReportItem
                {
                    EmpId = Convert.ToInt32(dr["c_empid"]),
                    Name = dr["c_name"]?.ToString() ?? string.Empty,
                    Email = dr["c_email"]?.ToString() ?? string.Empty,
                    Gender = dr["c_gender"]?.ToString() ?? string.Empty,
                    Status = dr["c_status"]?.ToString() ?? string.Empty,
                    Role = dr["c_role"]?.ToString() ?? string.Empty,
                    ProfileImage = dr["c_profileimage"] == DBNull.Value ? null : dr["c_profileimage"]?.ToString()
                });
            }

            return list;
        }

        public EmployeeReportItem? GetEmployeeById(int empid)
        {
            using var con = new NpgsqlConnection(_connectionString);
            const string query = @"SELECT c_empid, c_name, c_email, c_gender, c_status, c_role, c_profileimage
                                   FROM t_employees
                                   WHERE c_empid = @empid";

            using var cmd = new NpgsqlCommand(query, con);
            cmd.Parameters.AddWithValue("@empid", empid);
            con.Open();
            using var dr = cmd.ExecuteReader();

            if (!dr.Read())
            {
                return null;
            }

            return new EmployeeReportItem
            {
                EmpId = Convert.ToInt32(dr["c_empid"]),
                Name = dr["c_name"]?.ToString() ?? string.Empty,
                Email = dr["c_email"]?.ToString() ?? string.Empty,
                Gender = dr["c_gender"]?.ToString() ?? string.Empty,
                Status = dr["c_status"]?.ToString() ?? string.Empty,
                Role = dr["c_role"]?.ToString() ?? string.Empty,
                ProfileImage = dr["c_profileimage"] == DBNull.Value ? null : dr["c_profileimage"]?.ToString()
            };
        }

        public List<AttendanceReport> GetMonthlyReport(int empid, int month, int year)
        {
            List<AttendanceReport> list = new List<AttendanceReport>();

            using (var con = new NpgsqlConnection(_connectionString))
            {
                string query = @"SELECT * FROM t_attendance
                            WHERE c_empid=@empid
                            AND EXTRACT(MONTH FROM c_attenddate)=@month
                            AND EXTRACT(YEAR FROM c_attenddate)=@year";

                NpgsqlCommand cmd = new NpgsqlCommand(query, con);

                cmd.Parameters.AddWithValue("@empid", empid);
                cmd.Parameters.AddWithValue("@month", month);
                cmd.Parameters.AddWithValue("@year", year);

                con.Open();

                var dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    AttendanceReport r = new AttendanceReport();

                    r.AttendDate = Convert.ToDateTime(dr["c_attenddate"]);
                    r.ClockInHour = Convert.ToInt32(dr["c_clockinhour"]);
                    r.ClockInMin = Convert.ToInt32(dr["c_clockinmin"]);
                    r.ClockOutHour = Convert.ToInt32(dr["c_clockouthour"]);
                    r.ClockOutMin = Convert.ToInt32(dr["c_clockoutmin"]);
                    r.WorkingHour = Convert.ToInt32(dr["c_workinghour"]);
                    r.Status = dr["c_attendstatus"].ToString();
                    r.WorkType = dr["c_worktype"].ToString();
                    r.TaskType = dr["c_tasktype"].ToString();

                    list.Add(r);
                }
            }

            return list;
        }

        public List<AttendanceReport> GetYearlyReport(int empid, int year)
        {
            List<AttendanceReport> list = new List<AttendanceReport>();

            using (var con = new NpgsqlConnection(_connectionString))
            {
                string query = @"SELECT * FROM t_attendance
                                WHERE c_empid=@empid
                                AND EXTRACT(YEAR FROM c_attenddate)=@year
                                ORDER BY c_attenddate";

                NpgsqlCommand cmd = new NpgsqlCommand(query, con);
                cmd.Parameters.AddWithValue("@empid", empid);
                cmd.Parameters.AddWithValue("@year", year);

                con.Open();
                var dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    AttendanceReport r = new AttendanceReport
                    {
                        AttendDate = Convert.ToDateTime(dr["c_attenddate"]),
                        ClockInHour = Convert.ToInt32(dr["c_clockinhour"]),
                        ClockInMin = Convert.ToInt32(dr["c_clockinmin"]),
                        ClockOutHour = Convert.ToInt32(dr["c_clockouthour"]),
                        ClockOutMin = Convert.ToInt32(dr["c_clockoutmin"]),
                        WorkingHour = Convert.ToInt32(dr["c_workinghour"]),
                        Status = dr["c_attendstatus"]?.ToString() ?? string.Empty,
                        WorkType = dr["c_worktype"]?.ToString() ?? string.Empty,
                        TaskType = dr["c_tasktype"]?.ToString() ?? string.Empty
                    };

                    list.Add(r);
                }
            }

            return list;
        }
    }
}
