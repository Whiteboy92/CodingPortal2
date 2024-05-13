using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
namespace CodingPortal2.DbModels
{
    public class Plagiarism
    {
        [Key]
        public int PlagiarismId { get; set; }

        // Foreign key for the source solution
        [ForeignKey("UserSolutionId")]
        public int UserSolutionId { get; set; }

        // Navigation property for similar solutions
        [AllowNull]
        public List<PlagiarismEntry> PlagiarismEntries { get; set; }
        
        public UserAssignmentSolution UserAssignmentSolution { get; set; }
    }
}