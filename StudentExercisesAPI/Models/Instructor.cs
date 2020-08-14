using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace StudentExercisesAPI.Models
{
    public class Instructor
    {
        public int Id { get; set; }

        [Required]
        public string FirstName { get; set; }

         [Required]
       public string LastName { get; set; }

        [Required]
        [StringLength(12, MinimumLength = 3)]
        public string SlackHandle { get; set; }
        public int CohortId { get; set; }
        public Cohort Cohort { get; set; }

        public string Specialty { get; set; }


        public void AssignExerciseToStudent(Student victim, Exercise exerciseToAssign)
        {
            
            victim.AssignedExercises.Add(exerciseToAssign);

            exerciseToAssign.assignedStudnets.Add(victim);
        }

    }
}
