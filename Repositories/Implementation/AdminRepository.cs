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
                var query=@"SELECT COUNT(*) FROM t_employees e WHERE e.c_status = 'Active' AND e.c_empid NOT IN( SELECT c_empid FROM t_attendance WHERE c_attenddate = CURRENT_DATE);";
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
                var query = "select sum(c_workinghour) from t_attendance where c_tasktype ='Designing';";
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
                var query = "select sum(c_workinghour) from t_attendance where c_tasktype ='Developing';";
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
                var query = "select sum(c_workinghour) from t_attendance where c_tasktype ='Research';";
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
                var query = "SELECT COUNT(*) FROM t_employees";
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
            // try
            // {
            //     var query = "";
            //     using NpgsqlCommand cmd = new NpgsqlCommand(query, _conn);
            //     await _conn.CloseAsync();
            //     await _conn.OpenAsync();
            //     var reader = await cmd.ExecuteReaderAsync();
            //     if (reader.HasRows)
            //     {
            //         while (reader.Read())
            //         {
            //             var employee = new EmployeeModel
            //             {
            //                 EmpId=Convert.ToInt32(reader["c_empid"]),
                            
            //             }
            //         }
            //     }
            // }catch(Exception ex)
            // {
            //     Console.WriteLine($"Error: {ex.Message}");
            //     return null!;
            // }
            // finally
            // {
            //     await _conn.CloseAsync();
            // }
            throw new NotImplementedException();
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
                var query = "SELECT COUNT(DISTINCT c_empid) FROM t_attendance WHERE c_attenddate = CURRENT_DATE";
                
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

        public Task<int> UpdateEmpStatus()
        {
            throw new NotImplementedException();
        }
    }
}