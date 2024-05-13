using System.ComponentModel.DataAnnotations;
using CodingPortal2.DatabaseEnums;
namespace CodingPortal2.DbModels
{
    public class User
    {
        [Key]
        public int UserId { get; set; }
        public string Login { get; set; }
        public PermissionLevel PermissionLevel { get; set; }

        // Navigation properties
        public List<UserGroup> UserGroups { get; set; }
        public List<UserAssignmentDate> AssignedTasks { get; set; }
        public List<UserAssignmentSolution> UserAssignmentSolutions { get; set; }
        public List<Group> CreatedGroups { get; set; }
    }
}
