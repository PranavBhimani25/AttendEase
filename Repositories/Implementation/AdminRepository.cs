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

        public Task<int> CountDesigningHour()
        {
            throw new NotImplementedException();
        }

        public Task<int> CountDevelopingHour()
        {
            throw new NotImplementedException();
        }

        public Task<int> CountResearchHour()
        {
            throw new NotImplementedException();
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

        public Task<List<EmployeeModel>> ListEmployee()
        {
            throw new NotImplementedException();
        }

        public Task<int> OnLeaveEmpCount()
        {
            throw new NotImplementedException();
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