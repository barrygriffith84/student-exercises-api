using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Http;
using StudentExercisesAPI.Models;


namespace StudentExercisesAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CohortController : ControllerBase
    {
        private readonly IConfiguration _config;

        public CohortController(IConfiguration config)
        {
            _config = config;
        }

        public SqlConnection Connection
        {
            get
            {
                return new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Get(string name)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    string query = @"SELECT Cohort.Id, Cohort.Name, Student.Id AS 'Student Id', Student.FirstName,
Student.LastName, Student.SlackHandle, 
 Instructor.Id AS 'Instructor Id', Instructor.FirstName AS 'Instructor FirstName',
Instructor.LastName AS 'Instructor LastName', Instructor.SlackHandle AS 'Instructor SlackHandle', 
Instructor.CohortId AS 'Instructor CohortId' 
FROM Cohort  
LEFT JOIN Student ON Student.CohortId = Cohort.Id 
LEFT JOIN Instructor ON Instructor.CohortId = Cohort.Id
";

                    if (name != null)
                    {
                        query += $"WHERE Cohort.Name = '{name}' order by Cohort.Id";
                    }
                    else
                    {
                        query += $"order by Cohort.Id";
                    }
                 
                    cmd.CommandText = query;
                    SqlDataReader reader = cmd.ExecuteReader();
                    List<Cohort> cohorts = new List<Cohort>();

                    // Variable to store the id of the last Cohort added to the cohorts list
                    int LastCohortId = -1;

                    //While loop goes through each row returned in the database 
                    while (reader.Read())
                    {
                        //Checks the Cohort Id of the current row against LastCohortId
                        if (reader.GetInt32(reader.GetOrdinal("Id")) != LastCohortId)
                        {
                            //Instantiates a new Cohort and adds it to the cohorts list
                            Cohort cohort = new Cohort
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                Name = reader.GetString(reader.GetOrdinal("Name"))
                            };
                            cohorts.Add(cohort);

                            //Instantiates a new student and adds it to the students list
                            Student student = new Student
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Student Id")),
                                FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                                LastName = reader.GetString(reader.GetOrdinal("LastName")),
                                SlackHandle = reader.GetString(reader.GetOrdinal("SlackHandle"))
                            };
                            cohorts.Last().Students.Add(student);

                            //Instantiates a new Instructor and adds it to the instructors list
                            Instructor instructor = new Instructor
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Instructor Id")),
                                FirstName = reader.GetString(reader.GetOrdinal("Instructor FirstName")),
                                LastName = reader.GetString(reader.GetOrdinal("Instructor LastName")),
                                SlackHandle = reader.GetString(reader.GetOrdinal("Instructor SlackHandle"))
                            };
                            cohorts.Last().Instructors.Add(instructor);
                        }
                        else
                        {
                            //Checks to see if the current student returned from the database is in the Students list in the last Cohort in the cohorts list
                            if (!cohorts.Last().Students.Any(tempStudent => tempStudent.Id == reader.GetInt32(reader.GetOrdinal("Student Id"))))
                            {
                                //Instantiates a new Student and adds it to the students list in the last cohort in the cohorts list
                                Student student = new Student
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("Student Id")),
                                    FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                                    LastName = reader.GetString(reader.GetOrdinal("LastName")),
                                    SlackHandle = reader.GetString(reader.GetOrdinal("SlackHandle"))
                                };
                                cohorts.Last().Students.Add(student);
                            }

                            //Checks to see if the current instructor returned from the database is in the instructors list in the last cohort of the cohorts list
                            if (!cohorts.Last().Instructors.Any(tempInstructor => tempInstructor.Id == reader.GetInt32(reader.GetOrdinal("Instructor Id"))))
                            {
                                //Instantiates a new Instructor and adds it to the Instructors list in the last cohort of the cohorts list
                                Instructor instructor = new Instructor
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("Instructor Id")),
                                    FirstName = reader.GetString(reader.GetOrdinal("Instructor FirstName")),
                                    LastName = reader.GetString(reader.GetOrdinal("Instructor LastName")),
                                    SlackHandle = reader.GetString(reader.GetOrdinal("Instructor SlackHandle"))
                                };
                                cohorts.Last().Instructors.Add(instructor);
                            }
                        }

                        //LastCohortId is reassigned the value of the Id of the last cohort in the Cohorts list.
                        LastCohortId = cohorts.Last().Id;
                    }

                    reader.Close();

                    return Ok(cohorts);
                }
            }
        }

        [HttpGet("{id}", Name = "GetCohort")]
        public async Task<IActionResult> Get([FromRoute] int id)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT
                            Id, Name
                        FROM Cohort
                        WHERE Id = @id";
                    cmd.Parameters.Add(new SqlParameter("@id", id));
                    SqlDataReader reader = cmd.ExecuteReader();

                    Cohort cohort = null;

                    if (reader.Read())
                    {
                        cohort = new Cohort
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            Name = reader.GetString(reader.GetOrdinal("Name")),
                        };
                    }
                    reader.Close();

                    return Ok(cohort);
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Cohort cohort)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO Cohort (Name)
                                        OUTPUT INSERTED.Id
                                        VALUES (@name)";
                    cmd.Parameters.Add(new SqlParameter("@name", cohort.Name));
                   

                    int newId = (int)cmd.ExecuteScalar();
                    cohort.Id = newId;
                    return CreatedAtRoute("GetCohort", new { id = newId }, cohort);
                }
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put([FromRoute] int id, [FromBody] Cohort cohort)
        {
            try
            {
                using (SqlConnection conn = Connection)
                {
                    conn.Open();
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"UPDATE Cohort
                                            SET Name = @name
                                            WHERE Id = @id";
                        cmd.Parameters.Add(new SqlParameter("@name", cohort.Name));
                        cmd.Parameters.Add(new SqlParameter("@id", id));

                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            return new StatusCodeResult(StatusCodes.Status204NoContent);
                        }
                        throw new Exception("No rows affected");
                    }
                }
            }
            catch (Exception)
            {
                if (!CohortExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            try
            {
                using (SqlConnection conn = Connection)
                {
                    conn.Open();
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"DELETE FROM Cohort WHERE Id = @id";
                        cmd.Parameters.Add(new SqlParameter("@id", id));

                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            return new StatusCodeResult(StatusCodes.Status204NoContent);
                        }
                        throw new Exception("No rows affected");
                    }
                }
            }
            catch (Exception)
            {
                if (!CohortExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        private bool CohortExists(int id)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT Id, Name
                        FROM Cohort
                        WHERE Id = @id";
                    cmd.Parameters.Add(new SqlParameter("@id", id));

                    SqlDataReader reader = cmd.ExecuteReader();
                    return reader.Read();
                }
            }
        }
    }
}
