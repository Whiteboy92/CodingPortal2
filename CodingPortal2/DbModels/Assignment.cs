using System.ComponentModel.DataAnnotations;
using CodingPortal2.DatabaseEnums;
namespace CodingPortal2.DbModels
{
    public class Assignment
    {
        [Key]
        public int AssignmentId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public int TotalPoints { get; set; }
        public bool IsConfirmed { get; set; }
        public TimeSpan UploadFrequency { get; set; }
        public User Creator { get; set; }
        public string PathToTests { get; set; }
        public ProgrammingLanguage ProgrammingLanguage { get; set; }

        // Foreign key
        public int CreatorUserId { get; set; }

        // Navigation properties

        public List<UserAssignmentDate> AssignedUsers { get; set; }
        public List<UserAssignmentSolution> UserSolutions { get; set; }
        public List<Group> Groups { get; set; }
        public List<User> Users { get; set; }
    }
}