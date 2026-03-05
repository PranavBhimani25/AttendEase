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

        public async Task<EmployeeModel> Login(LoginModel model)
        {
            var query = "SELECT c_empid,c_email,c_role,c_status FROM t_employees WHERE c_email = @Email AND c_password = @Password AND c_status = 'Active'";
            try
            {
                await _connection.OpenAsync();
                using var command = new NpgsqlCommand(query, _connection);
                command.Parameters.AddWithValue("@Email", model.c_email);
                command.Parameters.AddWithValue("@Password", model.c_password);

                using var reader = await command.ExecuteReaderAsync();
                if(await reader.ReadAsync())
                {
                    return new EmployeeModel
                    {
                        c_empid = reader.GetInt32(reader.GetOrdinal("c_empid")),
                        c_email = reader.GetString(reader.GetOrdinal("c_email")),
                        c_role = reader.GetString(reader.GetOrdinal("c_role")),
                        c_status = reader.GetString(reader.GetOrdinal("c_status"))
                    };
                }
                else
                {
                    return null; 
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during login: {ex.Message}");
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
    }
}