using System.Data;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;

namespace API.Controllers
{
    [ApiController]
    public class PatientController : ControllerBase
    {
        public class Patient
        {
            public int PatientId { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public DateOnly Dob { get; set; }
            public string Gender { get; set; }
            public string Email { get; set; }
            public string Phonenumber { get; set; }
            public string EmergencyContact { get; set; }
            public string EmergencyContactInfo { get; set; }
            public decimal HeightCM { get; set; }
            public decimal WeightKG { get; set; }
            public decimal BMI { get; set; }
            public string ChronicConditions { get; set; }
            public string Allergies { get; set; }
            public string Medications { get; set; }
            public string FamilyHistory { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime UpdatedAt { get; set; }
        }
        private IConfiguration _configuration;
        public PatientController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        //getPatient
        [HttpGet("get_patients")]
        //[Authorize(Roles = "Admin,Doctor")]
        public JsonResult get_patients() 
        {
            string query = "SELECT * FROM patients";
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

        [HttpDelete("delete_patient/{id}")]
        //[Authorize(Roles = "Admin,Doctor")]
        public IActionResult delete_patients(int id)
        {
            string query = "DELETE FROM patients WHERE patientId = @id";
            string SqlDataSource = _configuration.GetConnectionString("mydb");
            using (SqlConnection mycon = new SqlConnection(SqlDataSource))
            {
                mycon.Open();
                string checkQuery = "SELECT COUNT(*) FROM patients WHERE patientId = @id";
                using (SqlCommand checkCommand = new SqlCommand(checkQuery, mycon))
                {
                    checkCommand.Parameters.AddWithValue("@id", id);
                    int patientExists = (int)checkCommand.ExecuteScalar();

                    if (patientExists == 0)
                    {
                        return NotFound(new { message = "Patient not found" });
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
                        return BadRequest(new { message = "Failed to delete patient" });
                    }
                }
            }
            
        }

        [HttpGet("patient-details/{id}")]
        //[Authorize(Roles = "Admin,Doctor")]
        public async Task<IActionResult> get_patient(int id)
        {
            string query = "SELECT * FROM patients where patientId = @id";
            Patient patient = null; 
            string SqlDataSource = _configuration.GetConnectionString("mydb");

            using (SqlConnection mycon = new SqlConnection(SqlDataSource))
            {
                await mycon.OpenAsync();
                using (SqlCommand command = new SqlCommand(query, mycon))
                {
                    command.Parameters.AddWithValue("@id", id);
                    using (SqlDataReader sqlDataReader = await command.ExecuteReaderAsync())
                    {
                        if (sqlDataReader.HasRows)
                        {
                            while (await sqlDataReader.ReadAsync())
                            {
                                // Map the result to the Patient model
                                patient = new Patient
                                {
                                    PatientId = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal("patientId")),
                                    FirstName = sqlDataReader.GetString(sqlDataReader.GetOrdinal("firstName")),
                                    LastName = sqlDataReader.GetString(sqlDataReader.GetOrdinal("lastName")),
                                    Dob= DateOnly.FromDateTime(sqlDataReader.GetDateTime(sqlDataReader.GetOrdinal("dob"))),
                                    Gender = sqlDataReader.GetString(sqlDataReader.GetOrdinal("gender")),
                                    Email = sqlDataReader.GetString(sqlDataReader.GetOrdinal("email")),
                                    Phonenumber = sqlDataReader.GetString(sqlDataReader.GetOrdinal("phonenumber")),
                                    EmergencyContact = sqlDataReader.GetString(sqlDataReader.GetOrdinal("emergencyContact")),
                                    EmergencyContactInfo = sqlDataReader.GetString(sqlDataReader.GetOrdinal("emergencyContactInfo")),
                                    HeightCM = sqlDataReader.GetDecimal(sqlDataReader.GetOrdinal("heightCM")),
                                    WeightKG = sqlDataReader.GetDecimal(sqlDataReader.GetOrdinal("weightKG")),
                                    BMI = sqlDataReader.GetDecimal(sqlDataReader.GetOrdinal("bmi")),
                                    ChronicConditions = sqlDataReader.IsDBNull(sqlDataReader.GetOrdinal("chronicConditions")) ? null : sqlDataReader.GetString(sqlDataReader.GetOrdinal("chronicConditions")),
                                    Allergies = sqlDataReader.IsDBNull(sqlDataReader.GetOrdinal("allergies")) ? null : sqlDataReader.GetString(sqlDataReader.GetOrdinal("allergies")),
                                    Medications = sqlDataReader.IsDBNull(sqlDataReader.GetOrdinal("medications")) ? null : sqlDataReader.GetString(sqlDataReader.GetOrdinal("medications")),
                                    FamilyHistory = sqlDataReader.IsDBNull(sqlDataReader.GetOrdinal("familyHistory")) ? null : sqlDataReader.GetString(sqlDataReader.GetOrdinal("familyHistory")),
                                    CreatedAt = sqlDataReader.GetDateTime(sqlDataReader.GetOrdinal("createdAt")),
                                    UpdatedAt = sqlDataReader.GetDateTime(sqlDataReader.GetOrdinal("updatedAt"))
                                };
                            }
                        }
                    }
                }
            }

            // If no patient was found, return an error or null
            if (patient == null)
            {
                return NotFound(new { message = "Patient not found" });
            }

            // Return the patient object as JSON
            return Ok(patient);
        }


        [HttpPut("/patient-details/update/{id}")]
        //[Authorize(Roles = "Admin,Doctor")]
        public async Task<JsonResult> update_patient(int id, [FromBody] Patient updatedpatient)
        {
            try
            {
                if (updatedpatient == null)
                {
                    return new JsonResult(new { message = "Invalid data provided" }) { StatusCode = 400 };
                }
                Console.WriteLine("Received ID: " + id);
                Console.WriteLine("Received Data: " + JsonConvert.SerializeObject(updatedpatient));
                string query = @"UPDATE patients
                     SET firstName = @firstName,
                         lastName = @lastName,
                         dob = @dob,
                         gender = @gender,
                         email = @email,
                         phonenumber = @phonenumber,
                         emergencyContact = @emergencyContact,
                         emergencyContactInfo = @emergencyContactInfo,
                         heightCM = @heightCM,
                         weightKG = @weightKG,
                         chronicConditions = @chronicConditions,
                         allergies = @allergies,
                         medications = @medications,
                         familyHistory = @familyHistory,
                         updatedAt = GETDATE()
                     WHERE patientId = @id";
                string SqlDataSource = _configuration.GetConnectionString("mydb");

                using (SqlConnection mycon = new SqlConnection(SqlDataSource))
                {
                    await mycon.OpenAsync();
                    using (SqlCommand command = new SqlCommand(query, mycon))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        command.Parameters.AddWithValue("@firstName", updatedpatient.FirstName);
                        command.Parameters.AddWithValue("@lastName", updatedpatient.LastName);
                        command.Parameters.AddWithValue("@dob", updatedpatient.Dob);
                        command.Parameters.AddWithValue("@gender", updatedpatient.Gender);
                        command.Parameters.AddWithValue("@email", updatedpatient.Email);
                        command.Parameters.AddWithValue("@phonenumber", updatedpatient.Phonenumber);
                        command.Parameters.AddWithValue("@emergencyContact", updatedpatient.EmergencyContact);
                        command.Parameters.AddWithValue("@emergencyContactInfo", updatedpatient.EmergencyContactInfo);
                        command.Parameters.AddWithValue("@heightCM", updatedpatient.HeightCM);
                        command.Parameters.AddWithValue("@weightKG", updatedpatient.WeightKG);
                        command.Parameters.AddWithValue("@chronicConditions", (object)updatedpatient.ChronicConditions ?? DBNull.Value);
                        command.Parameters.AddWithValue("@allergies", (object)updatedpatient.Allergies ?? DBNull.Value);
                        command.Parameters.AddWithValue("@medications", (object)updatedpatient.Medications ?? DBNull.Value);
                        command.Parameters.AddWithValue("@familyHistory", (object)updatedpatient.FamilyHistory ?? DBNull.Value);

                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        // If no rows were affected, return an error
                        if (rowsAffected == 0)
                        {
                            return new JsonResult(new { message = "Patient not found or no changes made" });
                        }
                    }

                }
                return new JsonResult(new { message = "Patient updated successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return new JsonResult(new { message = "An error occurred", error = ex.Message }) { StatusCode = 500 };
            }
        }

        [HttpPost("/add-patient/create")]
        //[Authorize(Roles = "Admin,Doctor")]
        public async Task<JsonResult> add_patient([FromBody] Patient newpatient)
        {
                string query = @"INSERT INTO patients
                        (firstName, lastName, dob, gender, email, phonenumber, emergencyContact, emergencyContactInfo, heightCM, weightKG, chronicConditions, allergies, medications, familyHistory, createdAt, updatedAt)
                         VALUES
                        (@firstName, @lastName, @dob, @gender, @email, @phonenumber, @emergencyContact, @emergencyContactInfo, @heightCM, @weightKG, @chronicConditions, @allergies, @medications, @familyHistory, GETDATE(), GETDATE())";
                    string SqlDataSource = _configuration.GetConnectionString("mydb");

                using (SqlConnection mycon = new SqlConnection(SqlDataSource))
                {
                    await mycon.OpenAsync();
                    using (SqlCommand command = new SqlCommand(query, mycon))
                    {
                        command.Parameters.AddWithValue("@firstName", newpatient.FirstName);
                        command.Parameters.AddWithValue("@lastName", newpatient.LastName);
                        command.Parameters.AddWithValue("@dob", newpatient.Dob);
                        command.Parameters.AddWithValue("@gender", newpatient.Gender);
                        command.Parameters.AddWithValue("@email", newpatient.Email);
                        command.Parameters.AddWithValue("@phonenumber", newpatient.Phonenumber);
                        command.Parameters.AddWithValue("@emergencyContact", newpatient.EmergencyContact);
                        command.Parameters.AddWithValue("@emergencyContactInfo", newpatient.EmergencyContactInfo);
                        command.Parameters.AddWithValue("@heightCM", newpatient.HeightCM);
                        command.Parameters.AddWithValue("@weightKG", newpatient.WeightKG);
                        command.Parameters.AddWithValue("@chronicConditions", (object)newpatient.ChronicConditions ?? DBNull.Value);
                        command.Parameters.AddWithValue("@allergies", (object)newpatient.Allergies ?? DBNull.Value);
                        command.Parameters.AddWithValue("@medications", (object)newpatient.Medications ?? DBNull.Value);
                        command.Parameters.AddWithValue("@familyHistory", (object)newpatient.FamilyHistory ?? DBNull.Value);

                        int rowsAffected = await command.ExecuteNonQueryAsync();

                     
                    }

                }
                return new JsonResult(new { message = "Patient added successfully" });
            }
           
        }

    }


    
    
