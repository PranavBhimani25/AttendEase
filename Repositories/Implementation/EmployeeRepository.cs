using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using Repositories.Interfaces;
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
                if(await reader.ReadAsync())
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
            catch(Exception e)
            {
                Console.WriteLine($"Error during Profile {e.Message}");
                return null;
            }
            finally
            {
                _conn.CloseAsync();
            }

        }



        public async Task<EmployeeModel> GetOne(int id)
        {
            var qry="SELECT * FROM t_employees WHERE c_empid=@id";
            EmployeeModel emp=new EmployeeModel();
            try
            {
                NpgsqlCommand cmd=new NpgsqlCommand(qry,_conn);
                cmd.Parameters.AddWithValue("@id",id);
                await _conn.OpenAsync();
                var reader=await cmd.ExecuteReaderAsync();
                if (reader.Read())
                {
                    emp.EmpId=(int)reader["c_empid"];
                    emp.Name=(string)reader["c_name"];
                    emp.Email=(string)reader["c_email"];
                    emp.Gender=(string)reader["c_gender"];
                    emp.Role=(string)reader["c_role"];
                    emp.Status=(string)reader["c_status"];
                    emp.ProfileImage=(string)reader["c_profileimage"];
                }
                await _conn.CloseAsync();
            }catch(Exception ex)
            {
                Console.WriteLine("Error is: "+ex.Message);
            }
            finally
            {
                await _conn.CloseAsync();
            }
            return emp;
        }



        public async Task<int> ChangePWD(int id,string pass)
        {
            var qry="UPDATE t_employees SET c_password=@pass WHERE c_empid=@id";
            try
            {
                NpgsqlCommand cmd=new NpgsqlCommand(qry,_conn);
                cmd.Parameters.AddWithValue("@id",id);
                cmd.Parameters.AddWithValue("@pass",pass);
                await _conn.OpenAsync();
                var status=await cmd.ExecuteNonQueryAsync();
                if (status == 1)
                {
                    Console.WriteLine("Password Chnage SuccessFully...");
                }
                await _conn.CloseAsync();
                return 1;
            }catch(Exception ex)
            {
                Console.WriteLine("Error is: "+ex.Message);
                return 0;
            }
            finally
            {
                await _conn.CloseAsync();
            }
        }



        public async Task<int> Update(UpdateEmployee emp)
        {
            var qry="UPDATE t_employees SET c_name=@name,c_email=@email,c_gender=@gender,c_profileimage=@image WHERE c_empid=@id";
            try
            {
                NpgsqlCommand cmd= new NpgsqlCommand(qry,_conn);
                cmd.Parameters.AddWithValue("@name",emp.Name);
                cmd.Parameters.AddWithValue("@email",emp.Email);
                cmd.Parameters.AddWithValue("@gender",emp.Gender);
                cmd.Parameters.AddWithValue("@image",emp.ProfileImageUrl);
                cmd.Parameters.AddWithValue("@id",emp.EmpId);

                await _conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
                await _conn.CloseAsync();
                return 1;
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error is: "+ex.Message);
                return 0;
            }
            finally
            {
                await _conn.CloseAsync();
            }
        }




        public async Task<List<vm_AttendenceUser>> GetAttendanceByEmployee(int empId,int month,int year)
        {
            List<vm_AttendenceUser> list=new List<vm_AttendenceUser>();

            var qry=@"SELECT c_attendid,c_empid,c_attenddate FROM t_attendance WHERE c_empid=@empid AND EXTRACT(MONTH FROM c_attenddate)=@month AND EXTRACT(YEAR FROM c_attenddate)=@year";
            try
            {
                NpgsqlCommand cmd=new NpgsqlCommand(qry,_conn);
                cmd.Parameters.AddWithValue("@empid",empId);
                cmd.Parameters.AddWithValue("@month",month);
                cmd.Parameters.AddWithValue("@year",year);

                await _conn.OpenAsync();

                var reader=await cmd.ExecuteReaderAsync();

                while (reader.Read())
                {
                    vm_AttendenceUser att = new vm_AttendenceUser();

                    att.AttendID=(int)reader["c_attendid"];
                    att.EmpID=(int)reader["c_empid"];
                    att.AttendDate=(DateOnly)reader["c_attenddate"];

                    att.Status="Present";

                    list.Add(att);
                }
                await _conn.CloseAsync();
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error : "+ex.Message);
            }
            finally
            {
                await _conn.CloseAsync();
            }
            return list;
        }



        public async Task<List<vm_YearlyWorkingHours>> GetYearlyWorkingHours(int empId,int year)
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
                NpgsqlCommand cmd = new NpgsqlCommand(qry,_conn);

                cmd.Parameters.AddWithValue("@empid",empId);
                cmd.Parameters.AddWithValue("@year",year);

                await _conn.OpenAsync();

                var reader = await cmd.ExecuteReaderAsync();

                while(await reader.ReadAsync())
                {
                    vm_YearlyWorkingHours obj = new vm_YearlyWorkingHours();

                    obj.Month = Convert.ToInt32(reader["month"]);
                    obj.MonthName = reader["monthname"].ToString().Trim();
                    obj.TotalHours = Convert.ToInt32(reader["totalhours"]);

                    list.Add(obj);
                }

                await reader.CloseAsync();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                await _conn.CloseAsync();
            }

            return list;
        }



        public async Task<List<vm_MonthlyWorkingHours>> GetMonthlyWorkingHours(int empId,int month,int year)
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

                    obj.Date =(DateOnly)reader["c_attenddate"];
                    obj.WorkingHour = Convert.ToInt32(reader["c_workinghour"]);

                    list.Add(obj);
                }

                await _conn.CloseAsync();
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error : " + ex.Message);
            }
            finally
            {
                await _conn.CloseAsync();
            }

            return list;
        }
    }
}