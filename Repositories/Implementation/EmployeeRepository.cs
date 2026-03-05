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
                        c_empid = reader.GetInt32(reader.GetOrdinal("c_empid")),
                        c_name = reader.GetString(reader.GetOrdinal("c_name")),
                        c_email = reader.GetString(reader.GetOrdinal("c_email")),
                        c_gender = reader.GetString(reader.GetOrdinal("c_gender")),
                        c_profileimage = reader.GetString(reader.GetOrdinal("c_profileimage")),
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
                    emp.c_empid=(int)reader["c_empid"];
                    emp.c_name=(string)reader["c_name"];
                    emp.c_email=(string)reader["c_email"];
                    emp.c_gender=(string)reader["c_gender"];
                    emp.c_role=(string)reader["c_role"];
                    emp.c_status=(string)reader["c_status"];
                    emp.c_profileimage=(string)reader["c_profileimage"];
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
    }
}