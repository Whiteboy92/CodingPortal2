using CodingPortal2.Database;
using CodingPortal2.DatabaseEnums;
using CodingPortal2.DbModels;
using CodingPortal2.Services;
using CodingPortal2.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
namespace CodingPortal2.Pages
{
    public class GroupListModel : PageModel
    {
        private readonly ApplicationDbContext dbContext;
        private readonly IMemoryCache memoryCache;
        private readonly GroupService groupService;
        private readonly string loggedAsUserLoginCacheKey = "LoggedAsUserLogin";
        
        public GroupListModel(ApplicationDbContext dbContext, IMemoryCache memoryCache, GroupService groupService)
        {
            this.dbContext = dbContext;
            this.memoryCache = memoryCache;
            this.groupService = groupService;
        }

        public string LoggedAsLogin = "";
        public string PermissionLevel { get; private set; }
        public string Login { get; set; } = "";
        public int UserId { get; set; }
        public List<Group> Groups { get; set; }

        public async Task OnGetAsync()
        {
            (PermissionLevel, Login, UserId) = UserHelper.GetUserProperties(HttpContext);
            PageHelper.GetTempDataMessagesAndSetToViewData(TempData, ViewData);
            Groups = await groupService.GetCreatorGroupsAsync(UserId);
            
            if(UserHelper.GetLoggedAsUserLogin(memoryCache, loggedAsUserLoginCacheKey) != "")
            {
                LoggedAsLogin = UserHelper.GetLoggedAsUserLogin(memoryCache, loggedAsUserLoginCacheKey);
                UserId = dbContext.Users.FirstOrDefault(user => user.Login == LoggedAsLogin)?.UserId ?? 0;
            }
        }

        public async Task<IActionResult> OnPostUpdateGroupAsync(int groupId, string year, Semester semester)
        {
            try
            {
                var groupToUpdate = await GetGroupByIdAsync(groupId);

                groupToUpdate.Year = year;
                groupToUpdate.Semester = semester;

                UpdateGroup(groupToUpdate);

                PageHelper.SetTempDataSuccessMessage("Group updated successfully", TempData);

                return RedirectToPage("/GroupList");
            }
            catch (Exception ex)
            {
                PageHelper.SetTempDataErrorMessage("An error occurred while updating the group.", TempData);
                return StatusCode(500, "An error occurred while updating the group.");
            }
        }

        public async Task<IActionResult> OnPostAsync(string action, List<Group> groups)
        {
            if (action == "UpdateGroup")
            {
                await UpdateGroupsAsync(groups);
                PageHelper.SetTempDataSuccessMessage("Groups updated successfully", TempData);
                return RedirectToPage("/GroupList");
            }

            return BadRequest();
        }

        private async Task<Group> GetGroupByIdAsync(int groupId)
        {
            return await dbContext.Groups.FindAsync(groupId) ?? throw new InvalidOperationException();
        }

        private void UpdateGroup(Group group)
        {
            dbContext.Groups.Update(group);
            dbContext.SaveChanges();
        }

        private async Task UpdateGroupsAsync(List<Group> groups)
        {
            foreach (var group in groups)
            {
                var groupToUpdate = await GetGroupByIdAsync(group.GroupId);

                groupToUpdate.Year = group.Year;
                groupToUpdate.Semester = group.Semester;

                UpdateGroup(groupToUpdate);
            }
        }
    }
}
