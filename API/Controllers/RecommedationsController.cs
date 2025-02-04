using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using static API.Controllers.PatientController;

namespace API.Controllers
{
    [ApiController]
    public class RecommedationsController : ControllerBase
    {

        public class RecommendedCheck
        {
            public int CheckId { get; set; }
            public string CheckName { get; set; }
            public string Description { get; set; }
            public DateTime CreatedAt { get; set; } 
            public DateTime? UpdatedAt { get; set; }
        }

        private IConfiguration _configuration;
        public RecommedationsController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        //getPatient
        [HttpGet("get_recommendedCheckslist")]
        [Authorize(Roles = "Admin,Doctor")]
        public JsonResult get_recommendedChecks()
        {
            string query = "SELECT * FROM recommendedChecks";
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
        [HttpDelete("recommendedChecks/{id}")]
        [Authorize(Roles = "Admin,Doctor")]
        public IActionResult delete_patients(int id)
        {
            string query = "DELETE FROM recommendedChecks WHERE CheckId = @id";
            string SqlDataSource = _configuration.GetConnectionString("mydb");
            using (SqlConnection mycon = new SqlConnection(SqlDataSource))
            {
                mycon.Open();
                string checkQuery = "SELECT COUNT(*) FROM recommendedChecks WHERE CheckId = @id";
                using (SqlCommand checkCommand = new SqlCommand(checkQuery, mycon))
                {
                    checkCommand.Parameters.AddWithValue("@id", id);
                    int patientExists = (int)checkCommand.ExecuteScalar();

                    if (patientExists == 0)
                    {
                        return NotFound(new { message = "recommendedCheck not found" });
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
                        return BadRequest(new { message = "Failed to delete check" });
                    }
                }
            }

        }
        //update
        [HttpPut("/recommendedChecks/update/{id}")]
        [Authorize(Roles = "Admin,Doctor")]
        public async Task<JsonResult> update_recommendedChecks(int id, [FromBody] RecommendedCheck updateRecommendedCheck)
        {
            try
            {
                if (updateRecommendedCheck == null)
                {
                    return new JsonResult(new { message = "Invalid data provided" }) { StatusCode = 400 };
                }

                string query = @"UPDATE recommendedChecks
                     SET CheckName = @CheckName,
                         Description = @Description,
                         updatedAt = GETDATE()
                     WHERE CheckId = @id";
                string SqlDataSource = _configuration.GetConnectionString("mydb");

                using (SqlConnection mycon = new SqlConnection(SqlDataSource))
                {
                    await mycon.OpenAsync();
                    using (SqlCommand command = new SqlCommand(query, mycon))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        command.Parameters.AddWithValue("@CheckName", updateRecommendedCheck.CheckName);
                        command.Parameters.AddWithValue("@Description", updateRecommendedCheck.Description);
                       

                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        // If no rows were affected, return an error
                        if (rowsAffected == 0)
                        {
                            return new JsonResult(new { message = "RecommendedCheck not found or no changes made" });
                        }
                    }

                }
                return new JsonResult(new { message = "RecommendedCheck updated successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return new JsonResult(new { message = "An error occurred", error = ex.Message }) { StatusCode = 500 };
            }
        }
        //create
        [HttpPost("/recommendedChecks/create")]
        [Authorize(Roles = "Admin,Doctor")]
        public async Task<JsonResult> add_RecommendedCheck([FromBody] RecommendedCheck newRecommendedCheck)
        {
            string query = @"INSERT INTO recommendedChecks
                        (CheckName, Description, createdAt, updatedAt)
                         VALUES
                        (@CheckName, @Description,GETDATE(), GETDATE())";
            string SqlDataSource = _configuration.GetConnectionString("mydb");

            using (SqlConnection mycon = new SqlConnection(SqlDataSource))
            {
                await mycon.OpenAsync();
                using (SqlCommand command = new SqlCommand(query, mycon))
                {
                    command.Parameters.AddWithValue("@CheckName", newRecommendedCheck.CheckName);
                    command.Parameters.AddWithValue("@Description", newRecommendedCheck.Description);
                    int rowsAffected = await command.ExecuteNonQueryAsync();


                }

            }
            return new JsonResult(new { message = "RecommendedCheck added successfully" });
        }


    }
}
