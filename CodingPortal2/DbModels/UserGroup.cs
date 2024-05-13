using System.ComponentModel.DataAnnotations.Schema;
namespace CodingPortal2.DbModels
{
    public class UserGroup
    {
        [ForeignKey("UserId")]
        public int UserId { get; set; }
        
        [ForeignKey("GroupId")]
        public int GroupId { get; set; }

        // Navigation properties
        public User User { get; set; }
        public Group Group { get; set; }
    }
}