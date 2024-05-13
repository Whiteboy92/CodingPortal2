using System.ComponentModel.DataAnnotations.Schema;
namespace CodingPortal2.DbModels;

public class UserAssignmentDate
{
    [ForeignKey("UserId")]
    public int UserId { get; set; }

    [ForeignKey("AssignmentId")]
    public int AssignmentId { get; set; }
    
    public DateTimeOffset AssignmentTime { get; set; }
    public DateTimeOffset DeadLineDateTime { get; set; }
    public DateTimeOffset LastUploadDateTime { get; set; }
    public TimeSpan TimeToNextUpload { get; set; }

    // Navigation properties
    public User User { get; set; }
    public Assignment Assignment { get; set; }
}
