using CodingPortal2.DatabaseEnums;
using CodingPortal2.DbModels;
namespace CodingPortal2.Database
{
    public class SeedData
    {
        public static void Initialize(ApplicationDbContext context)
        {
            context.Database.EnsureCreated();

            //create Admin
            if (!context.Users.Any(user1 => user1.Login == "Admin"))
            {
                var user2 = new User
                {
                    Login = "Admin",
                    PermissionLevel = PermissionLevel.Admin
                };
                
                context.Users.Add(user2);
                context.SaveChanges();
            }

            // check if group exists and if not, create it
            if (!context.Groups.Any(group1 => group1.Code == "Administration"))
            {
                var group0 = new Group
                {
                    Code = "Administration",
                    Year = "2023/2024",
                    Semester = Semester.Summer,
                    CreatorUserId = context.Users.FirstOrDefault(user1 => user1.Login == "Admin")?.UserId ?? 0,
                };

                context.Groups.Add(group0);
                context.SaveChanges();
            }

            
            //add user Admin to group Administration
            var user = context.Users.FirstOrDefault(user1 => user1.Login == "Admin"); 
            var group = context.Groups.FirstOrDefault(group1 => group1.Code == "Administration");
            if (user != null && group != null && !context.UserGroups.Any(userGroup => userGroup.UserId == user.UserId && userGroup.GroupId == group.GroupId))
            {
                var userGroup = new UserGroup
                {
                    UserId = user.UserId,
                    GroupId = group.GroupId
                };

                context.UserGroups.Add(userGroup);
                context.SaveChanges();
            }
        }
    }
}
