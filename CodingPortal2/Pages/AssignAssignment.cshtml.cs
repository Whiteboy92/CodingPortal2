using CodingPortal2.Database;
using CodingPortal2.DbModels;
using CodingPortal2.Services;
using CodingPortal2.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
namespace CodingPortal2.Pages;

public class AssignAssignmentModel : PageModel
{
    private readonly ApplicationDbContext dbContext;
    private readonly IMemoryCache memoryCache;
    private readonly AssignmentService assignmentService;
    private readonly GroupService groupService;
    private readonly string loggedAsUserLoginCacheKey = "LoggedAsUserLogin";
    
    public AssignAssignmentModel(ApplicationDbContext dbContext, IMemoryCache memoryCache, AssignmentService assignmentService, GroupService groupService)
    {
        this.dbContext = dbContext;
        this.memoryCache = memoryCache;
        this.assignmentService = assignmentService;
        this.groupService = groupService;
    }

    public string LoggedAsLogin = "";
    public string PermissionLevel { get; private set; }
    public string Login { get; set; } = "";
    public int UserId { get; set; }
    public List<Group> GroupsOfCreator { get; set; }
    public List<User> Users { get; set; }
    public Assignment Assignment { get; set; }

    
    public async Task<IActionResult> OnGetAsync(int assignmentId)
    {
        try
        {
            (PermissionLevel, Login, UserId) = UserHelper.GetUserProperties(HttpContext);
            if (UserHelper.GetLoggedAsUserLogin(memoryCache, loggedAsUserLoginCacheKey) != "")
            {
                LoggedAsLogin = UserHelper.GetLoggedAsUserLogin(memoryCache, loggedAsUserLoginCacheKey);
                UserId = dbContext.Users.FirstOrDefault(user => user.Login == LoggedAsLogin)?.UserId ?? 0;
            }

            GroupsOfCreator = await groupService.GetCreatorGroupsAsync(UserId);
            Users = dbContext.Users.ToList();
            
            Assignment = (await assignmentService.GetAssignmentByIdAsync(assignmentId))!;

            return Page();
        }
        catch
        {
            return StatusCode(500, "An error occurred while processing the request.");
        }
    }
    
    public async Task<IActionResult> OnPostAsync(string action)
    {
        (PermissionLevel, Login, UserId) = UserHelper.GetUserProperties(HttpContext);

        return action switch
        {
            "AssignAssignment" => await OnPostAssignAssignment(),
            _ => RedirectToPage("/AddUsers")
        };
    }
    
    private async Task<IActionResult> OnPostAssignAssignment()
    {
        if (!int.TryParse(Request.Form["AssignmentId"], out var assignmentId)) { return NotFound(); }

        Assignment? assignment = await assignmentService.GetAssignmentByIdAsync(assignmentId);
        if (assignment == null) { return NotFound(); }

        await ConfirmNotConfirmedTask(assignment);

        List<int> selectedUserIds = Request.Form["SelectedUserLogins"].Select(int.Parse).ToList();
        List<string>? selectedGroupCodes = Request.Form["SelectedGroupCodes"].ToList();
        List<int> overdueUserIds = GetUserIdsWithOverdueAssignment(assignment);
        await DeleteUserAssignmentDatesAsync(overdueUserIds, assignmentId);

        int days = ParseAndValidateInput(Request.Form["Days"]);
        int hours = ParseAndValidateInput(Request.Form["Hours"]);
        int minutes = ParseAndValidateInput(Request.Form["Minutes"]);
        TimeSpan timeToCompleteTask = TimeHelper.CalculateTimeSpanDaysHoursMinutes(days, hours, minutes);
    
        List<int> userIds = GetFinalUserIds(assignment, selectedGroupCodes, selectedUserIds);

        await AssignAssignmentToUserIds(userIds, assignmentId, timeToCompleteTask);

        var groups1 = GetGroupsForUserIds(userIds);

        var groupCodes = selectedGroupCodes.Select(code => code.Trim()).ToList();
        var groups2 = dbContext.Groups
            .Include(group => group.AssignmentsInGroup)
            .Where(group => groupCodes.Contains(group.Code))
            .ToList();

        var groups = groups1.Union(groups2).ToList();

        foreach (var group in groups)
        {
            group.AssignmentsInGroup ??= new List<Assignment>();

            var assignmentsInGroup = dbContext.Assignments
                .Include(assignment1 => assignment1.Groups)
                .Any(assignment1 => assignment1.AssignmentId == assignment.AssignmentId && assignment1.Groups.Any(group1 => group1.GroupId == group.GroupId));

            if (!assignmentsInGroup)
            {
                group.AssignmentsInGroup.Add(assignment);
            }
        }

        await dbContext.SaveChangesAsync();
        return RedirectToPage("/Index");
    }
    
    private List<Group> GetGroupsForUserIds(List<int> userIds)
    {
        var groups = dbContext.Groups
            .Include(group => group.UserGroups)
            .Where(group => group.UserGroups.Any(userGroup => userIds.Contains(userGroup.UserId)))
            .ToList();

        return groups;
    }
    
    private async Task ConfirmNotConfirmedTask(Assignment assignment)
    {
        if (!assignment.IsConfirmed)
        {
            assignment.IsConfirmed = true;
            await dbContext.SaveChangesAsync();
        }
    }

    private List<int> GetFinalUserIds(Assignment assignment, List<string>? selectedGroupCodes, List<int> selectedUserIds)
    {
        List<int> userIdsAlreadyAssignedToTask = GetUserIdsAlreadyAssigned(assignment);

        List<int> userIdsFromGroups = new List<int>();
        
        if(selectedGroupCodes != null && selectedGroupCodes.Any())
        {
            userIdsFromGroups = GetUserIdsFromGroups(selectedGroupCodes
                .Select(code => code.Trim())
                .ToList());
        }
        
        selectedUserIds.Remove(assignment.CreatorUserId);

        List<int> userIds = selectedUserIds
            .Union(userIdsFromGroups)
            .Except(userIdsAlreadyAssignedToTask)
            .ToList();

        return userIds;
    }
    
    private async Task<Assignment?> AssignAssignmentToUserIds(List<int> userIds, int assignmentId, TimeSpan timeToCompleteTask)
    {
        var assignmentTime = DateTimeOffset.Now;

        foreach (int userId in userIds)
        {
            var existingUserAssignmentDate = dbContext.UserAssignmentDates
                .FirstOrDefault(userAssignmentDate => userAssignmentDate.UserId == userId && userAssignmentDate.AssignmentId == assignmentId);

            if (existingUserAssignmentDate != null)
            {
                existingUserAssignmentDate.DeadLineDateTime = assignmentTime.Add(timeToCompleteTask);
                existingUserAssignmentDate.LastUploadDateTime = DateTimeOffset.MinValue;
            }
            else
            {
                var newUserAssignmentDate = new UserAssignmentDate
                {
                    UserId = userId,
                    AssignmentId = assignmentId,
                    AssignmentTime = assignmentTime,
                    LastUploadDateTime = DateTimeOffset.MinValue,
                    DeadLineDateTime = assignmentTime.Add(timeToCompleteTask)
                };

                dbContext.UserAssignmentDates.Add(newUserAssignmentDate);
            }
        }

        await dbContext.SaveChangesAsync();

        PageHelper.SetTempDataSuccessMessage("Task assigned successfully", TempData);
        return null;
    }

    
    private List<int> GetUserIdsAlreadyAssigned(Assignment assignment)
    {
        var currentDateTime = DateTimeOffset.Now;

        List<int> alreadyAssignedUserIds = dbContext.UserAssignmentDates
            .Where(userAssignmentDate => 
                userAssignmentDate.AssignmentId == assignment.AssignmentId &&
                userAssignmentDate.DeadLineDateTime > currentDateTime)  // Exclude users with deadlines that have passed
            .Select(userAssignmentDate => userAssignmentDate.UserId)
            .ToList();

        return alreadyAssignedUserIds;
    }


    private List<int> GetUserIdsFromGroups(List<string>? groupCodes)
    {
        List<User> userIdsExtractedFromGroups = new List<User>();
    
        if (groupCodes != null && groupCodes.Any())
        {
            Dictionary<string, int> groupCodeToIdMap = dbContext.Groups.ToDictionary(group => group.Code, group => group.GroupId);

            foreach (string code in groupCodes)
            {
                if (groupCodeToIdMap.TryGetValue(code, out int groupId))
                {
                    List<User> userIdsFromGroup = dbContext.UserGroups
                        .Where(userGroup => userGroup.GroupId == groupId)
                        .Select(userGroup => userGroup.User)
                        .ToList();
                    userIdsExtractedFromGroups.AddRange(userIdsFromGroup);
                }
            }
        }

        List<int> userIdsFromGroups = userIdsExtractedFromGroups.Select(user => user.UserId).ToList();
        return userIdsFromGroups;
    }
    
    private List<int> GetUserIdsWithOverdueAssignment(Assignment assignment)
    {
        var currentDateTime = DateTimeOffset.Now;
        
        List<int> overdueUserIds = dbContext.UserAssignmentDates
            .Where(userAssignmentDate =>
                userAssignmentDate.AssignmentId == assignment.AssignmentId &&
                userAssignmentDate.DeadLineDateTime <= currentDateTime)
            .Select(userAssignmentDate => userAssignmentDate.UserId)
            .ToList();

        return overdueUserIds;
    }
    
    public async Task DeleteUserAssignmentDatesAsync(List<int> userIds, int assignmentId)
    {
        var userAssignmentDates = dbContext.UserAssignmentDates
            .Where(userAssignmentDate => userIds.Contains(userAssignmentDate.UserId) && userAssignmentDate.AssignmentId == assignmentId)
            .ToList();

        dbContext.UserAssignmentDates.RemoveRange(userAssignmentDates);
        await dbContext.SaveChangesAsync();
    }
    
    protected virtual int ParseAndValidateInput(string input)
    {
        if (int.TryParse(input?.Trim(), out var value) && value >= 0)
        {
            return value;
        }
        
        return 0;
    }
}