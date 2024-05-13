using System.ComponentModel.DataAnnotations;
namespace CodingPortal2.DbModels;

public class PlagiarismEntry
{
    [Key]
    public int PlagiarismEntryId { get; set; }
    public int PlagiarisedSolutionId { get; set; }
    public double Percentage { get; set; }
    
    public UserAssignmentSolution PlagiarisedSolution { get; set; }
}