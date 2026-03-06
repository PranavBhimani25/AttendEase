using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Repositories.Interfaces;
using Repositories.Models;
using Npgsql;


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
            finally
            {
                await _conn.CloseAsync();
            }
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
                return Convert.ToInt32(result);                
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
    }
}