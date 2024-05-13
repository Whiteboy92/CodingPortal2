using System.ComponentModel.DataAnnotations;
using CodingPortal2.DatabaseEnums;
namespace CodingPortal2.DbModels
{
    public class Group
    {
        [Key]
        public int GroupId { get; set; }
        public string Code { get; set; }
        public string Year { get; set; }
        public Semester Semester { get; set; }
        public int CreatorUserId { get; set; }

        // Navigation properties
        public List<UserGroup> UserGroups { get; set; }
        public User Creator { get; set; }
        public List<Assignment> AssignmentsInGroup { get; set; }
    }
}