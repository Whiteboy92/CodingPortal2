using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
namespace CodingPortal2.DbModels
{
    public class UserAssignmentSolution
    { 
        [Key]
        public int UserAssignmentSolutionId { get; set; }

        public int AssignmentId { get; set; }
        public int UserId { get; set; }
        public int PlagiarismId { get; set; }
        public string Solution { get; set; }
        public DateTimeOffset UploadDateTime { get; set; }
        public int TestPassed { get; set; }
        public int TotalTests { get; set; }
        
        // Navigation properties
        public Assignment Assignment { get; set; }
        public User User { get; set; }
        
        [AllowNull]
        public Plagiarism Plagiarism { get; set; }
    }
}