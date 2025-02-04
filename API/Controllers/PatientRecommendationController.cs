using System.Data;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using static API.Controllers.PatientController;
using static API.Controllers.UsersController;

namespace API.Controllers
{
    [ApiController]
    public class PatientRecommendationController : ControllerBase
    {
        public class PatientRecommendation
        {
            public int id { get; set; }
            public int patientId { get; set; }
            public int checkId { get; set; }
            public bool completed { get; set; }
            public DateTime createdAt { get; set; }
            public DateTime updatedAt { get; set; }

        }

        private IConfiguration _configuration;
        public PatientRecommendationController(IConfiguration configuration)
        {
            _configuration = configuration;
        }


        //getallchecks
        [HttpGet("get_allPatientChecks")]
        [Authorize(Roles = "Admin,Doctor")]
        public async Task<IActionResult> get_allPatientChecks()
        {
            // Query updated to use @id parameter for consistency
            string query = "SELECT pr.id, pr.patientId, pr.completed, rc.CheckName AS CheckName,pr.updatedAt,pr.createdAt " +
                           "FROM patientRecommendation pr " +
                           "JOIN recommendedChecks rc ON pr.checkId = rc.Checkid "
                           ;

            DataTable dt = new DataTable();
            string SqlDataSource = _configuration.GetConnectionString("mydb");

            using (SqlConnection mycon = new SqlConnection(SqlDataSource))
            {
                await mycon.OpenAsync();
                using (SqlCommand command = new SqlCommand(query, mycon))
                {

                    using (SqlDataReader sqlDataReader = await command.ExecuteReaderAsync())
                    {
                        dt.Load(sqlDataReader);
                    }
                }
            }

            // Return the result after the SQL connection and reader are done
            return new JsonResult(dt);
        }


        //addpatientcheck
        [HttpPost("assignCheck")]
        //[Authorize(Roles = "Admin,Doctor")]
        public async Task<JsonResult> assignCheck([FromBody] List<PatientRecommendation> newChecks)
        {
            string query = @"
        INSERT INTO patientRecommendation (patientId, checkId, completed, createdAt, updatedAt)
        VALUES (@patientId, @checkId, @completed, GETDATE(), GETDATE());";

            string SqlDataSource = _configuration.GetConnectionString("mydb");

            using (SqlConnection mycon = new SqlConnection(SqlDataSource))
            {
                await mycon.OpenAsync();

                foreach (var newCheck in newChecks)
                {

                    using (SqlCommand command = new SqlCommand(query, mycon))
                    {
                        command.Parameters.AddWithValue("@patientId", newCheck.patientId);
                        command.Parameters.AddWithValue("@checkId", newCheck.checkId);
                        command.Parameters.AddWithValue("@completed", newCheck.completed);

                        await command.ExecuteNonQueryAsync();
                    }
                }
            }

            return new JsonResult(new { message = "PatientRecommendations added successfully" });
        }


        //updateatientcheck
        [HttpPut("updateCheckStatuses")]
        [Authorize(Roles = "Admin,Doctor")]
        public async Task<JsonResult> updateCheckStatuses([FromBody] List<PatientRecommendation> updatedChecks)
        {
            try
            {
                string query = @"UPDATE patientRecommendation SET completed = @completed,updatedAt = GETDATE() WHERE id =@id";
                string SqlDataSource = _configuration.GetConnectionString("mydb");

                using (SqlConnection mycon = new SqlConnection(SqlDataSource))
                {
                    await mycon.OpenAsync();
                    using (SqlTransaction transaction = mycon.BeginTransaction()) 
                    {
                        try
                        {
                            foreach (var check in updatedChecks)
                            {
                                using (SqlCommand command = new SqlCommand(query, mycon, transaction))
                                {
                                    command.Parameters.AddWithValue("@id", check.id);
                                    command.Parameters.AddWithValue("@completed", check.completed);

                                    await command.ExecuteNonQueryAsync();
                                }
                            }

                            transaction.Commit(); 
                        }
                        catch (Exception)
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }

                return new JsonResult(new { message = "Patient recommendations updated successfully" });



            }
            catch (Exception ex)
            {
                return new JsonResult(new { message = "An error occurred", error = ex.Message }) { StatusCode = 500 };
            }

        }


        //getpatientcheck
        [HttpGet("get_PatientChecks/{id}")]
        [Authorize(Roles = "Admin,Doctor")]
        public async Task<IActionResult> get_PatientChecks(int id)
        {
            // Query updated to use @id parameter for consistency
            string query = "SELECT pr.id, pr.patientId, pr.completed, rc.CheckName AS CheckName,pr.updatedAt " +
                           "FROM patientRecommendation pr " +
                           "JOIN recommendedChecks rc ON pr.checkId = rc.Checkid " +
                           "WHERE pr.patientId = @id;";

            DataTable dt = new DataTable();
            string SqlDataSource = _configuration.GetConnectionString("mydb");

            using (SqlConnection mycon = new SqlConnection(SqlDataSource))
            {
                await mycon.OpenAsync();
                using (SqlCommand command = new SqlCommand(query, mycon))
                {
                    // Add the parameter to the command, use @id to match the query
                    command.Parameters.AddWithValue("@id", id);

                    using (SqlDataReader sqlDataReader = await command.ExecuteReaderAsync())
                    {
                        dt.Load(sqlDataReader);
                    }
                }
            }

            // Return the result after the SQL connection and reader are done
            return new JsonResult(dt);
        }

        [HttpGet("get_PatientsByCheck/{id}")]
        [Authorize(Roles = "Admin,Doctor")]
        public async Task<IActionResult> get_PatientsByCheck(int checkId)
        {
            string query = "SELECT pr.id, pr.patientId, pr.completed, " +
                           "p.firstname + ' ' + p.lastname AS fullName, rc.CheckName " +
                           "FROM patientrecommendation pr " +
                           "JOIN recommendedChecks rc ON pr.checkId = rc.Checkid " +
                           "JOIN patients p ON pr.patientId = p.patientId " +
                           "WHERE pr.checkId = @checkId;";

            DataTable dt = new DataTable();
            string SqlDataSource = _configuration.GetConnectionString("mydb");

            using (SqlConnection mycon = new SqlConnection(SqlDataSource))
            {
                await mycon.OpenAsync();
                using (SqlCommand command = new SqlCommand(query, mycon))
                {
                    command.Parameters.AddWithValue("@checkId", checkId);

                    using (SqlDataReader sqlDataReader = await command.ExecuteReaderAsync())
                    {
                        dt.Load(sqlDataReader);
                    }
                }
            }

            return new JsonResult(dt);
        }


    }
}
