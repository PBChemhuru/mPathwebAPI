using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;



namespace API.Controllers
{
    [ApiController]
    public class LoginController : ControllerBase
    {
        private IConfiguration _configuration;

        public class LoginRequest
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }

        public LoginController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        //get users
        [HttpGet("get_users")]
        public JsonResult get_user()
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

        [HttpPost("add_users")]
        public JsonResult add_user([FromForm] string firstname, [FromForm] string surname, [FromForm] string email, [FromForm] string password_hash, [FromForm] string role)
        {
            string username = surname.ToLower() + firstname.ToLower();
            if (IsUsernameExists(username))
            {
                return new JsonResult(new { message = "Username already exists." }) { StatusCode = 400 };
            }

            string query = "INSERT INTO users (username, email, password_hash, firstname, surname , role, refresh_token, created_at, updated_at) " +
                    "VALUES (@username, @email, @password_hash, @firstname,@surname, @role, NULL, GETDATE(), GETDATE())";
            string SqlDataSource = _configuration.GetConnectionString("mydb");
            string password = BCrypt.Net.BCrypt.HashPassword(password_hash);

            using (SqlConnection mycon = new SqlConnection(SqlDataSource))
            {
                mycon.Open();
                using (SqlCommand command = new SqlCommand(query, mycon))
                {
                    command.Parameters.AddWithValue("@username", username);
                    command.Parameters.AddWithValue("@firstname", firstname);
                    command.Parameters.AddWithValue("@surname", surname);
                    command.Parameters.AddWithValue("@email", email);
                    command.Parameters.AddWithValue("@password_hash", password);
                    command.Parameters.AddWithValue("@role", string.IsNullOrEmpty(role) ? "user" : role); 
                    command.Parameters.AddWithValue("@refresh_token", DBNull.Value);
                    command.ExecuteNonQuery();
                }
            }

            return new JsonResult(new { Message = "User added successfully", Username = username });
        }


        private bool IsUsernameExists(string username)
        {
            string query = "SELECT COUNT(*) FROM users WHERE username = @username";
            string SqlDataSource = _configuration.GetConnectionString("mydb");

            using (SqlConnection mycon = new SqlConnection(SqlDataSource))
            {
                using (SqlCommand command = new SqlCommand(query, mycon))
                {
                    command.Parameters.AddWithValue("@username", username);
                    mycon.Open();
                    int count = (int)command.ExecuteScalar();
                    return count > 0;
                }
            }

        }


        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
        {
            try
            {

                string username = loginRequest.Username;
                string password = loginRequest.Password;
                string query = "SELECT password_hash FROM users WHERE username = @username";
                string SqlDataSource = _configuration.GetConnectionString("mydb");
                using (SqlConnection mycon = new SqlConnection(SqlDataSource))
                {
                    await mycon.OpenAsync();
                    using (SqlCommand command = new SqlCommand(query, mycon))
                    {
                        command.Parameters.AddWithValue("@username", username);
                        var result = await command.ExecuteScalarAsync();
                        if (result == null)
                        {
                            return Unauthorized(new { message = "Invalid username or password." });

                        }
                        string storedHashPassword = result.ToString();

                        if (BCrypt.Net.BCrypt.Verify(password, storedHashPassword))
                        {
                            string userDetailsQuery = "SELECT firstname, surname, role FROM users WHERE username = @username";
                            using (SqlCommand userCommand = new SqlCommand(userDetailsQuery, mycon))
                            {
                                userCommand.Parameters.AddWithValue("@username", username);
                                using (SqlDataReader reader = await userCommand.ExecuteReaderAsync())
                                {
                                    if (await reader.ReadAsync())
                                    {
                                        string firstname = reader["firstname"].ToString();
                                        string surname = reader["surname"].ToString();
                                        string role = reader["role"].ToString();

                                        // Generate the JWT token with the user's claims
                                        string token = GenerateJwtToken(username, firstname, surname, role);

                                        return Ok(new { message = "Login successful", token });
                                    }
                                    else
                                    {
                                        return Unauthorized(new { message = "Invalid username or password." });
                                    }
                                }
                            }
                        }
                        else
                        {
                            return Unauthorized(new { message = "Invalid username or password." });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred during the login process.", error = ex.Message });

            }

        }


        private string GenerateJwtToken(string username, string firstname, string surname, string role)
        {

            // Get the secret key, issuer, and audience from the configuration
            var secretKey = _configuration["JwtSettings:SecretKey"];
            var issuer = _configuration["JwtSettings:Issuer"];
            var audience = _configuration["JwtSettings:Audience"];
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, username), 
                new Claim(ClaimTypes.GivenName, firstname),
                new Claim(ClaimTypes.Surname, surname), 
                new Claim(ClaimTypes.Role, role)

            };

            // Define the key using the secret
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            // Create the signing credentials with the key and HMAC SHA-256 algorithm
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            // Define the expiration time for the token
            var expiration = DateTime.UtcNow.AddHours(1);
            // Create the JWT token
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiration,
                signingCredentials: credentials
            );

            // Return the JWT token as a string
            return new JwtSecurityTokenHandler().WriteToken(token);

        }

    }






}
