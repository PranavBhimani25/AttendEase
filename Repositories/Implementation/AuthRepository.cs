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
            var query = @"INSERT INTO t_employees (c_name, c_email, c_password, c_gender, c_status, c_profileimage, c_role)
                          VALUES (@Name, @Email, @Password, @Gender, @Status, @ProfileImage, @Role)
                          RETURNING c_empid";
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
                var result = await command.ExecuteScalarAsync();
                return result == null ? -1 : Convert.ToInt32(result);
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