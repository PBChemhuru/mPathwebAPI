using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using static API.Controllers.RecommedationsController;

namespace API.Controllers
{
    [ApiController]
    public class UsersController : ControllerBase
    {
        public class User
        {
            public int id { get; set; } 
            public string username { get; set; }
            public string email { get; set; }
            public string password_hash { get; set; }
            public string? role { get; set; }
            public DateTime CreatedAt { get; set; } 
            public DateTime? UpdatedAt { get; set; }
            public string firstname { get; set; }
            public string surname { get; set; }
            public int? PatientId { get; set; }
        }

        private IConfiguration _configuration;
        public UsersController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        //getPatient
        [HttpGet("get_users")]
        [Authorize(Roles = "Admin")]
        public JsonResult get_users()
        {
            string query = "SELECT * FROM users";
            DataTable dt = new DataTable();
            string SqlDataSource = _configuration.GetConnectionString("mydb");
            SqlDataReader sqlDataReader;
            using (SqlConnection mycon = new SqlConnection(SqlDataSource))
            {
                mycon.Open();
                using (SqlCommand command = new SqlCommand(query, mycon))
                {
                    sqlDataReader = command.ExecuteReader();
                    dt.Load(sqlDataReader);
                }
            }
            // Return the result after the SQL connection and reader are done
            return new JsonResult(dt);
        }
        //delete
        [HttpDelete("user/{id}")]
        [Authorize(Roles = "Admin")]
        public IActionResult delete_user(int id)
        {
            string query = "DELETE FROM users WHERE id = @id";
            string SqlDataSource = _configuration.GetConnectionString("mydb");
            using (SqlConnection mycon = new SqlConnection(SqlDataSource))
            {
                mycon.Open();
                string checkQuery = "SELECT COUNT(*) FROM users WHERE Id = @id";
                using (SqlCommand checkCommand = new SqlCommand(checkQuery, mycon))
                {
                    checkCommand.Parameters.AddWithValue("@id", id);
                    int patientExists = (int)checkCommand.ExecuteScalar();

                    if (patientExists == 0)
                    {
                        return NotFound(new { message = "user not found" });
                    }
                }

                using (SqlCommand command = new SqlCommand(query, mycon))
                {
                    command.Parameters.AddWithValue("@id", id);
                    int rowsAffected = command.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        return Ok(new { message = "Deleted successfully" });
                    }
                    else
                    {
                        return BadRequest(new { message = "Failed to delete user" });
                    }
                }
            }

        }
        //update
        [HttpPut("/user/update/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<JsonResult> update_user(int id, [FromBody] User updateUser)
        {
            try
            {
                if (updateUser == null)
                {
                    return new JsonResult(new { message = "Invalid data provided" }) { StatusCode = 400 };
                }

                string query = @"UPDATE users
                     SET email = @email,
                         password_hash = @password_hash,
                         firstname = @firstname,
                         surname = @surname,
                        username = @username,    
                        updated_at = GETDATE()
                     WHERE id = @id";
                string SqlDataSource = _configuration.GetConnectionString("mydb");

                string password = BCrypt.Net.BCrypt.HashPassword(updateUser.password_hash);
                using (SqlConnection mycon = new SqlConnection(SqlDataSource))
                {
                    await mycon.OpenAsync();
                    using (SqlCommand command = new SqlCommand(query, mycon))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        command.Parameters.AddWithValue("@email", updateUser.email);
                        command.Parameters.AddWithValue("@firstname", updateUser.firstname);
                        command.Parameters.AddWithValue("@surname", updateUser.surname);
                        command.Parameters.AddWithValue("@username", updateUser.username);
                        command.Parameters.AddWithValue("@password_hash", password);

                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        // If no rows were affected, return an error
                        if (rowsAffected == 0)
                        {
                            return new JsonResult(new { message = "User not found or no changes made" });
                        }
                    }

                }
                return new JsonResult(new { message = "User updated successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return new JsonResult(new { message = "An error occurred", error = ex.Message }) { StatusCode = 500 };
            }
        }
        //create
        [HttpPost("/user/create")]
        [Authorize(Roles = "Admin")]
        public async Task<JsonResult> add_user([FromBody] User newUser)
        {
            string query = @"INSERT INTO users
                     (email, password_hash, role, created_at, updated_at, firstname, surname, username, patientId)
                     VALUES 
                     (@Email, @PasswordHash, @Role, GETDATE(), GETDATE(), @Firstname, @Surname, @Username, @PatientId);";
            string SqlDataSource = _configuration.GetConnectionString("mydb");
            string password = BCrypt.Net.BCrypt.HashPassword(newUser.password_hash);
            using (SqlConnection mycon = new SqlConnection(SqlDataSource))
            {
                await mycon.OpenAsync();
                using (SqlCommand command = new SqlCommand(query, mycon))
                {
                    command.Parameters.AddWithValue("@Username", newUser.username);
                    command.Parameters.AddWithValue("@Email", newUser.email);
                    command.Parameters.AddWithValue("@PasswordHash", password);
                    command.Parameters.AddWithValue("@Role", newUser.role);
                    command.Parameters.AddWithValue("@Firstname", newUser.firstname);
                    command.Parameters.AddWithValue("@Surname", newUser.surname);
                    if (newUser.role == "Patient" && newUser.PatientId.HasValue)
                    {
                        command.Parameters.AddWithValue("@PatientId", newUser.PatientId);
                    }
                    else
                    {
                        command.Parameters.AddWithValue("@PatientId", DBNull.Value);
                    }

                    int rowsAffected = await command.ExecuteNonQueryAsync();


                }

            }
            return new JsonResult(new { message = "User added successfully" });
        }
    }
}
