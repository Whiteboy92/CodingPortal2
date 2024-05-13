using CodingPortal2.Database;
using CodingPortal2.DatabaseEnums;
using CodingPortal2.DbModels;
using CodingPortal2.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Task = System.Threading.Tasks.Task;

namespace CodingPortal2.Pages
{
    public class UserListModel : PageModel
    {
        private readonly ApplicationDbContext dbContext;
        private readonly IMemoryCache memoryCache;
        private readonly string loggedAsUserLoginCacheKey = "LoggedAsUserLogin";

        public UserListModel(ApplicationDbContext dbContext, IMemoryCache memoryCache)
        {
            this.dbContext = dbContext;
            this.memoryCache = memoryCache;
        }

        public string LoggedAsLogin = "";
        public List<User> Users { get; set; }
        public string PermissionLevel { get; private set; }
        public string Login { get; set; } = "";
        public int UserId { get; set; }
        public List<Group> Groups { get; set; }

        public async Task OnGetAsync()
        {
            (PermissionLevel, Login, UserId) = UserHelper.GetUserProperties(HttpContext);
            Groups = await dbContext.Groups.ToListAsync();
            PageHelper.GetTempDataMessagesAndSetToViewData(TempData, ViewData);
            
            if(UserHelper.GetLoggedAsUserLogin(memoryCache, loggedAsUserLoginCacheKey) != "")
            {
                LoggedAsLogin = UserHelper.GetLoggedAsUserLogin(memoryCache, loggedAsUserLoginCacheKey);
                UserId = dbContext.Users.FirstOrDefault(user => user.Login == LoggedAsLogin)?.UserId ?? 0;
            }
            
            await LoadUsersAsync();
        }

        public async Task<IActionResult> OnPostAddToGroupAsync(int userId, string? selectedGroup)
        {
            if (selectedGroup == null)
            {
                HandleInvalidGroupSelection();
            }

            var user = await GetUserWithGroupsAsync(userId);
            if (selectedGroup != null)
            {
                var group = await GetGroupByCodeAsync(selectedGroup);

                if (UserAlreadyInGroup(user, group.GroupId))
                {
                    HandleUserAlreadyInGroup();
                }

                AddUserToGroup(user, group);
            }
            await dbContext.SaveChangesAsync();

            PageHelper.SetTempDataSuccessMessage("User added to the group successfully.", TempData);
            return RedirectToPage("/UserList");
        }

        public async Task<IActionResult> OnPostUpdatePermissionAsync(int userId, PermissionLevel permissionLevel)
        {
            var user = await GetUserByIdAsync(userId);

            user.PermissionLevel = permissionLevel;
            await dbContext.SaveChangesAsync();

            PageHelper.SetTempDataSuccessMessage("Permission updated successfully.", TempData);
            return RedirectToPage("/UserList");
        }

        private async Task LoadUsersAsync()
        {
            Users = await dbContext.Users
                .Include(user => user.UserGroups)
                .ThenInclude(userGroup => userGroup.Group)
                .ToListAsync();
        }

        private void HandleInvalidGroupSelection()
        {
            PageHelper.SetTempDataErrorMessage("No group selected / No group Found.", TempData);
            RedirectToPage("/UserList");
        }

        private async Task<Group> GetGroupByCodeAsync(string code)
        {
            return await dbContext.Groups.FirstOrDefaultAsync(g => g.Code == code) ?? throw new InvalidOperationException();
        }

        private async Task<User> GetUserWithGroupsAsync(int userId)
        {
            return await dbContext.Users
                .Include(user1 => user1.UserGroups)
                .FirstOrDefaultAsync(user1 => user1.UserId == userId) ?? throw new InvalidOperationException();
        }

        private void HandleUserAlreadyInGroup()
        {
            PageHelper.SetTempDataErrorMessage("User is already in the selected group.", TempData);
            RedirectToPage("/UserList");
        }

        private void AddUserToGroup(User user, Group group)
        {
            user.UserGroups.Add(new UserGroup
            {
                UserId = user.UserId,
                GroupId = group.GroupId
            });
        }

        private bool UserAlreadyInGroup(User user, int groupId)
        {
            return user.UserGroups.Any(userGroup => userGroup.GroupId == groupId);
        }

        private async Task<User> GetUserByIdAsync(int userId)
        {
            return await dbContext.Users.FindAsync(userId) ?? throw new InvalidOperationException();
        }
        
        public async Task<IActionResult> OnPostLoginAsUserAsync(int userId)
        {
            var userToLoginAs = await dbContext.Users.FindAsync(userId);

            if (userToLoginAs == null) { return NotFound(); }
            
            var userPermissionLevel = HttpContext.Session.GetString("PermissionLevel");
            
            switch (userPermissionLevel)
            {
                case "Admin" when userToLoginAs.PermissionLevel == DatabaseEnums.PermissionLevel.Admin:
                    PageHelper.SetTempDataErrorMessage("Permission denied. Admins can't login as other Admins.", TempData);
                    return RedirectToPage("/UserList");
                
                case "Teacher" when userToLoginAs.PermissionLevel != DatabaseEnums.PermissionLevel.Student:
                    PageHelper.SetTempDataErrorMessage("Permission denied. Teachers can only login as students.", TempData);
                    return RedirectToPage("/UserList");
            }
            
            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromSeconds(120),
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
                Priority = CacheItemPriority.Normal,
            };
            
            memoryCache.Set(loggedAsUserLoginCacheKey, userToLoginAs.Login, cacheEntryOptions);
            PageHelper.SetTempDataSuccessMessage($"Logged as {userToLoginAs.Login} successfully.", TempData);
            return RedirectToPage("/Index");
        }
    }
}
