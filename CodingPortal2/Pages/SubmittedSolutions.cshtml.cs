using CodingPortal2.Database;
using CodingPortal2.DatabaseEnums;
using CodingPortal2.DbModels;
using CodingPortal2.PlagiarismDetection;
using CodingPortal2.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
namespace CodingPortal2.Pages;

public class SubmittedSolutionsModel : PageModel
{
    private readonly ApplicationDbContext dbContext;
    private readonly IMemoryCache memoryCache;
    private readonly string loggedAsUserLoginCacheKey = "LoggedAsUserLogin";
    
    public SubmittedSolutionsModel(ApplicationDbContext dbContext, IMemoryCache memoryCache)
    {
        this.dbContext = dbContext;
        this.memoryCache = memoryCache;
    }
    
    [BindProperty(SupportsGet = true)]
    public bool ConfirmDeleteSolution { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public bool ConfirmDeletionAllSolutions { get; set; }
    public string LoggedAsLogin = "";
    public string? PermissionLevel { get; private set; }
    public string Login { get; set; } = "";
    public int UserId { get; set; }
    public List<Assignment> AssignmentsOfUserId { get; set; }
    public List<User> AllStudentUsers { get; set; }
    public Dictionary<Group, Dictionary<Assignment, Dictionary<User, List<UserAssignmentSolution>>>> AssignmentsWithSolutions { get; set; }
    
    
    public void OnGetAsync()
    {
        (PermissionLevel, Login, UserId) = UserHelper.GetUserProperties(HttpContext);
        PageHelper.GetTempDataMessagesAndSetToViewData(TempData, ViewData);

        if (UserHelper.GetLoggedAsUserLogin(memoryCache, loggedAsUserLoginCacheKey) != "")
        {
            LoggedAsLogin = UserHelper.GetLoggedAsUserLogin(memoryCache, loggedAsUserLoginCacheKey);
            UserId = dbContext.Users.FirstOrDefault(user => user.Login == LoggedAsLogin)?.UserId ?? 0;
        }
        
        AllStudentUsers = dbContext.Users
            .Where(user => user.PermissionLevel == DatabaseEnums.PermissionLevel.Student &&
                           user.UserAssignmentSolutions.Any())
            .ToList();
        
        AssignmentsOfUserId = GetAssignmentsForUser(UserId);
        AssignmentsWithSolutions = GetAssignmentsInGroupsWithUserSolutions();
    }
    
    public async Task<IActionResult> OnPostAsync(string action, int userAssignmentSolutionId, int userId, int assignmentId)
    {
        (PermissionLevel, Login, UserId) = UserHelper.GetUserProperties(HttpContext);

        return action switch
        {
            "View" => await OnPostViewCode(userAssignmentSolutionId),
            "DeleteSolution" => await OnPostDeleteSolution(userId, assignmentId, userAssignmentSolutionId),
            "DeleteAllSolutions" => await OnPostDeleteAllSolutions(userId, userAssignmentSolutionId),
            _ => RedirectToPage("/SubmittedSolutions")
        };
    }
    
    public async Task<IActionResult> OnPostRunPlagiarismCheck(int assignmentId)
    {
        DockerInitializationPlagiarism dockerInitializationPlagiarism = new DockerInitializationPlagiarism(dbContext);
        var assignment = dbContext.Assignments.FirstOrDefault(a => a.AssignmentId == assignmentId);

        if (assignment == null) return RedirectToPage("/SubmittedSolutions");
        
        ProgrammingLanguage programmingLanguage = assignment.ProgrammingLanguage;

        string? resultUrl = await dockerInitializationPlagiarism.DetectPlagiarismsForAssignment(programmingLanguage, assignmentId);
        var fetchHttpData = new FetchHttpData(dbContext);
        if (resultUrl != null) await fetchHttpData.ProcessPlagiarismDataAsync(resultUrl);

        PageHelper.SetTempDataSuccessMessage("Plagiarism data ready!" + resultUrl, TempData);
        return RedirectToPage("/SubmittedSolutions");
    }

    public Dictionary<Group, Dictionary<Assignment, Dictionary<User, List<UserAssignmentSolution>>>> GetAssignmentsInGroupsWithUserSolutions()
    {
        // Retrieve groups where the logged-in user is the creator
        var groupsWithAssignmentsAndSolutions = dbContext.Groups
            .Include(group => group.AssignmentsInGroup)
            .ThenInclude(assignment => assignment.AssignedUsers)
            .ThenInclude(userAssignmentDate => userAssignmentDate.User)
            .ThenInclude(user => user.UserAssignmentSolutions)
            .ThenInclude(plagiarism => plagiarism.Plagiarism)
            .ThenInclude(plagiarismEntry => plagiarismEntry.PlagiarismEntries)
            .Include(group => group.AssignmentsInGroup)
            .ThenInclude(assignment => assignment.AssignedUsers)
            .ThenInclude(userAssignmentDate => userAssignmentDate.User)
            .ThenInclude(user => user.UserGroups)
            .Where(group => group.CreatorUserId == UserId) // Filter groups by creator
            .ToList();

        var result = new Dictionary<Group, Dictionary<Assignment, Dictionary<User, List<UserAssignmentSolution>>>>();

        foreach (var group in groupsWithAssignmentsAndSolutions)
        {
            var assignmentsWithSolutions = new Dictionary<Assignment, Dictionary<User, List<UserAssignmentSolution>>>();

            foreach (var assignment in group.AssignmentsInGroup)
            {
                var usersWithSolutions = assignment.AssignedUsers
                    .Where(userAssignmentDate => userAssignmentDate.User.UserGroups.Any(userGroup => userGroup.GroupId == group.GroupId))
                    .SelectMany(userAssignmentDate => userAssignmentDate.User.UserAssignmentSolutions
                        .Where(solution => solution.AssignmentId == assignment.AssignmentId)
                        .ToList())
                    .GroupBy(solution => solution.User)
                    .ToDictionary(grouping => grouping.Key, grouping => grouping.ToList());

                assignmentsWithSolutions.Add(assignment, usersWithSolutions);
            }

            result.Add(group, assignmentsWithSolutions);
        }

        return result;
    }

    public List<Assignment> GetAssignmentsForUser(int userId)
    {
        var assignmentsWithSolutions = dbContext.Assignments
            .Include(assignment => assignment.UserSolutions)
            .Where(assignment => assignment.AssignedUsers.Any(userAssignmentDate => userAssignmentDate.UserId == userId) &&
                                 assignment.UserSolutions.Any(userAssignmentSolution => userAssignmentSolution.UserId == userId))
            .ToList();

        return assignmentsWithSolutions;
    }
    
    public Task<IActionResult> OnPostViewCode(int userAssignmentSolutionId)
    {
        (PermissionLevel, Login, UserId) = UserHelper.GetUserProperties(HttpContext);

        TempData["ViewCode"] = dbContext.UserAssignmentSolutions
            .FirstOrDefault(userAssignmentSolution => userAssignmentSolution.UserAssignmentSolutionId == userAssignmentSolutionId)
            ?.Solution;

        return Task.FromResult<IActionResult>(RedirectToPage("/ViewSolutionCode", new { userAssignmentSolutionId }));
    }
    
    public async Task<IActionResult> OnPostDeleteSolution(int userId, int assignmentId, int userAssignmentSolutionId)
    {
        if (ConfirmDeleteSolution)
        {
            var userLogin = dbContext.Users.FirstOrDefault(user => user.UserId == userId)?.Login;
            var assignmentTitle = dbContext.Assignments.FirstOrDefault(assignment => assignment.AssignmentId == assignmentId)?.Title;

            var solutionToDelete = await dbContext.UserAssignmentSolutions.FindAsync(userAssignmentSolutionId);

            if (solutionToDelete == null)
            {
                PageHelper.SetTempDataErrorMessage("Solution not found.", TempData);
                return RedirectToPage("/SubmittedSolutions");
            }

            var plagiarismEntriesToDelete = dbContext.PlagiarismEntries
                .Where(entry => entry.PlagiarisedSolutionId == userAssignmentSolutionId)
                .ToList();

            dbContext.PlagiarismEntries.RemoveRange(plagiarismEntriesToDelete);
            dbContext.UserAssignmentSolutions.Remove(solutionToDelete);

            await dbContext.SaveChangesAsync();

            PageHelper.SetTempDataSuccessMessage($"Solution for user {userLogin} and assignment {assignmentTitle} deleted.", TempData);
            return RedirectToPage("/SubmittedSolutions");
        }

        return Page();
    }
    

    public async Task<IActionResult> OnPostDeleteAllSolutions(int userId, int assignmentId)
    {
        if (ConfirmDeletionAllSolutions)
        {
            var solutionsToDelete = dbContext.UserAssignmentSolutions
                .Where(solution => solution.UserId == userId &&
                                   solution.AssignmentId == assignmentId &&
                                   (solution.TestPassed != solution.TotalTests ||
                                    solution.TestPassed == 0))
                .ToList();

            foreach (var solutionToDelete in solutionsToDelete)
            {
                // Remove associated PlagiarismEntries
                var plagiarismEntriesToDelete = dbContext.PlagiarismEntries
                    .Where(entry => entry.PlagiarisedSolutionId == solutionToDelete.UserAssignmentSolutionId)
                    .ToList();

                dbContext.PlagiarismEntries.RemoveRange(plagiarismEntriesToDelete);
            }
            
            dbContext.UserAssignmentSolutions.RemoveRange(solutionsToDelete);

            await dbContext.SaveChangesAsync();

            var userLogin = dbContext.Users.FirstOrDefault(user => user.UserId == userId)?.Login;
            var assignmentTitle = dbContext.Assignments.FirstOrDefault(assignment => assignment.AssignmentId == assignmentId)?.Title;

            PageHelper.SetTempDataSuccessMessage($"All solutions for user {userLogin} and assignment {assignmentTitle} deleted.", TempData);
            return RedirectToPage("/SubmittedSolutions");
        }

        // If ConfirmDeletionAllSolutions is not true, return to the page without deleting
        return Page();
    }


}