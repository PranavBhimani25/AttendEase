using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using Repositories.Interfaces;
using Repositories.Models;

namespace Repositories.Implementation
{
    public class AuthRepository : IAuthInterface
    {
        private readonly NpgsqlConnection _connection;
        public AuthRepository(NpgsqlConnection connection)
        {
            _connection = connection;
        }
        public Task<string> ChangePassword(ChangePasswordModel model)
        {
            throw new NotImplementedException();
        }

        public async Task<EmployeeModel?> Login(LoginModel model)
        {
            var query = @"SELECT c_empid,c_email,c_role,c_status,c_password 
                        FROM t_employees 
                        WHERE c_email = @Email";

            try
            {
                await _connection.OpenAsync();

                using var command = new NpgsqlCommand(query, _connection);
                command.Parameters.AddWithValue("@Email", model.c_email);

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var password = reader.GetString(reader.GetOrdinal("c_password"));

                    if (password != model.c_password)
                        return null;

                    return new EmployeeModel
                    {
                        EmpId = reader.GetInt32(reader.GetOrdinal("c_empid")),
                        Email = reader.GetString(reader.GetOrdinal("c_email")),
                        Role = reader.GetString(reader.GetOrdinal("c_role")),
                        Status = reader.GetString(reader.GetOrdinal("c_status"))
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login error: {ex.Message}");
                return null;
            }
            finally
            {
                await _connection.CloseAsync();
            }
        }

        public Task<string> Logout(string token)
        {
            throw new NotImplementedException();
        }

        public async Task<int> Register(RegisterEmployeeModel model)
        {
            var query = "INSERT INTO t_employees (c_name, c_email, c_password, c_gender, c_status, c_profileimage, c_role) VALUES (@Name, @Email, @Password, @Gender, @Status, @ProfileImage, @Role) ";
            try
            {
                using var command = new NpgsqlCommand(query, _connection);
                command.Parameters.AddWithValue("@Name", model.c_name);
                command.Parameters.AddWithValue("@Email", model.c_email);
                command.Parameters.AddWithValue("@Password", model.c_password);
                command.Parameters.AddWithValue("@Gender", NpgsqlTypes.NpgsqlDbType.Varchar, model.c_gender);
                command.Parameters.AddWithValue("@Status", NpgsqlTypes.NpgsqlDbType.Varchar, "Inactive");
                command.Parameters.AddWithValue("@ProfileImage", model.ProfileImageUrl ?? string.Empty);
                command.Parameters.AddWithValue("@Role", NpgsqlTypes.NpgsqlDbType.Varchar, "Employee");

                await _connection.OpenAsync();
                var result = await command.ExecuteNonQueryAsync();
                return result;
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error during registration: {ex.Message}");
                return -1;
            }
            finally
            {
                await _connection.CloseAsync();
            }
            
        }



        public async Task<int> ForgetPassword(LoginModel model)
        {
            try
            {
                await _connection.OpenAsync();

                // Step 1: Check if email exists
                var checkQry = "SELECT c_email FROM t_employees WHERE c_email=@email";
                using (var checkCmd = new NpgsqlCommand(checkQry, _connection))
                {
                    checkCmd.Parameters.AddWithValue("@email", model.c_email);

                    var exists = await checkCmd.ExecuteReaderAsync();
                    Console.WriteLine("Es" + exists);
                    if (!exists.HasRows)
                    {
                        return -1; 
                    }
                }

                await _connection.CloseAsync();
                await _connection.OpenAsync();
                
                // Step 2: Update password
                var updateQry = "UPDATE t_employees SET c_password=@pass WHERE c_email=@email";
                using (var updateCmd = new NpgsqlCommand(updateQry, _connection))
                {
                    updateCmd.Parameters.AddWithValue("@email", model.c_email);
                    updateCmd.Parameters.AddWithValue("@pass", model.c_password);

                    var status = await updateCmd.ExecuteNonQueryAsync();

                    if (status == 1)
                    {
                        return 1; 
                    }
                    else
                    {
                        return 0; 
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return 0;
            }
            finally
            {
                await _connection.CloseAsync();
            }
        }



        public async Task<int> GetOne(string email)
        {
            var qry="SELECT * FROM t_employees WHERE c_email=@email";
            EmployeeModel emp=new EmployeeModel();
            try
            {
                NpgsqlCommand cmd=new NpgsqlCommand(qry,_connection);
                cmd.Parameters.AddWithValue("@email", email);
                await _connection.OpenAsync();
                var reader=await cmd.ExecuteReaderAsync();
                if (!reader.HasRows)
                {
                    return 0;
                }
                return 1;
            }catch(Exception ex)
            {
                Console.WriteLine("Error is: "+ex.Message);
            }
            finally
            {
                await _connection.CloseAsync();
            }
            return 0;
        }



    }
}