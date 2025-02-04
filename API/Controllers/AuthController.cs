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
    [Route("api/auth")]
    public class LoginController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly string _audience;

        public LoginController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("mydb");
            _secretKey = _configuration["JwtSettings:SecretKey"];
            _issuer = _configuration["JwtSettings:Issuer"];
            _audience = _configuration["JwtSettings:Audience"];
        }

        public class LoginRequest
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }

        public class LogoutRequest
        {
            public string Token { get; set; }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
        {
            if (string.IsNullOrEmpty(loginRequest.Username) || string.IsNullOrEmpty(loginRequest.Password))
                return BadRequest(new { message = "Username and password are required." });

            try
            {
                string storedHashPassword = await GetPasswordHashAsync(loginRequest.Username);
                if (string.IsNullOrEmpty(storedHashPassword) || !BCrypt.Net.BCrypt.Verify(loginRequest.Password, storedHashPassword))
                {
                    return Unauthorized(new { message = "Invalid username or password." });
                }

                var userDetails = await GetUserDetailsAsync(loginRequest.Username);
                if (userDetails == null)
                {
                    return Unauthorized(new { message = "Invalid user data." });
                }

                // Ensure userDetails is not null
                var userDetailsNonNull = userDetails.Value;

                string token = GenerateJwtToken(userDetailsNonNull);
                return Ok(new { message = "Login successful", token });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred during login.", error = ex.Message });
            }
        }


        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest logoutRequest)
        {
            if (string.IsNullOrEmpty(logoutRequest.Token))
                return BadRequest(new { message = "Token is required." });

            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(logoutRequest.Token) as JwtSecurityToken;

            if (jsonToken == null)
                return Unauthorized(new { message = "Invalid token." });

            var expirationDate = jsonToken.ValidTo;

            await BlacklistTokenAsync(logoutRequest.Token, expirationDate);

            return Ok(new { message = "Logout successful." });
        }

        private async Task<string> GetPasswordHashAsync(string username)
        {
            string query = "SELECT password_hash FROM users WHERE username = @username";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@username", username);
                    var result = await command.ExecuteScalarAsync();
                    return result?.ToString();
                }
            }
        }

        private async Task<(string Firstname, string Surname, string Role, string Id)?> GetUserDetailsAsync(string username)
        {
            string query = "SELECT firstname, surname, role, id FROM users WHERE username = @username";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@username", username);
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return (
                                reader["firstname"].ToString(),
                                reader["surname"].ToString(),
                                reader["role"].ToString(),
                                reader["id"].ToString()
                            );
                        }
                    }
                }
            }
            return null;
        }

        private string GenerateJwtToken((string Firstname, string Surname, string Role, string Id) userDetails)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, userDetails.Firstname),
                new Claim(ClaimTypes.GivenName, userDetails.Firstname),
                new Claim(ClaimTypes.Surname, userDetails.Surname),
                new Claim(ClaimTypes.Role, userDetails.Role),
                new Claim("Id", userDetails.Id)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiration = DateTime.UtcNow.AddHours(1);

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: expiration,
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private async Task BlacklistTokenAsync(string token, DateTime expirationDate)
        {
            string query = "INSERT INTO BlacklistedTokens (Token, ExpirationDate, DateBlacklisted) VALUES (@Token, @ExpirationDate, @DateBlacklisted)";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Token", token);
                    command.Parameters.AddWithValue("@ExpirationDate", expirationDate);
                    command.Parameters.AddWithValue("@DateBlacklisted", DateTime.UtcNow);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        private async Task<bool> IsTokenBlacklistedAsync(string token)
        {
            string query = "SELECT COUNT(*) FROM BlacklistedTokens WHERE Token = @Token";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Token", token);
                    var result = await command.ExecuteScalarAsync();
                    return Convert.ToInt32(result) > 0;
                }
            }
        }
    }
}
