using CodingPortal2.Database;
using CodingPortal2.DbModels;
using CodingPortal2.Shared;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
namespace CodingPortal2.Pages;

public class PlagiarismInfo : PageModel
{
    private readonly ApplicationDbContext dbContext;
    private readonly IMemoryCache memoryCache;
    private readonly string loggedAsUserLoginCacheKey = "LoggedAsUserLogin";
    
    public PlagiarismInfo(ApplicationDbContext dbContext, IMemoryCache memoryCache)
    {
        this.dbContext = dbContext;
        this.memoryCache = memoryCache;
    }
    
    public string? PermissionLevel { get; private set; }
    public string Login { get; set; } = "";
    public int UserId { get; set; }
    public string LoggedAsLogin = "";
    public List<int> PlagiarizedIds { get; set; }
    public int CheckedSolutionId { get; set; }
    public UserAssignmentSolution CheckedSolutionData { get; set; }
    public List<UserAssignmentSolution> PlagiarisedSolutionsData { get; set; }
    public List<User> UsersData { get; set; }
    
    public void OnGetAsync(int checkedSolutionId, string plagiarizedSolutionIds)
    {
        (PermissionLevel, Login, UserId) = UserHelper.GetUserProperties(HttpContext);
        PageHelper.GetTempDataMessagesAndSetToViewData(TempData, ViewData);

        if (UserHelper.GetLoggedAsUserLogin(memoryCache, loggedAsUserLoginCacheKey) != "")
        {
            LoggedAsLogin = UserHelper.GetLoggedAsUserLogin(memoryCache, loggedAsUserLoginCacheKey);
            UserId = dbContext.Users.FirstOrDefault(user => user.Login == LoggedAsLogin)?.UserId ?? 0;
        }

        PlagiarizedIds = plagiarizedSolutionIds.Split(',').Select(int.Parse).ToList();
        CheckedSolutionId = checkedSolutionId;

        CheckedSolutionData = dbContext.UserAssignmentSolutions
            .Include(userAssignmentSolution => userAssignmentSolution.Assignment)
            .Include(userAssignmentSolution => userAssignmentSolution.User)
            .FirstOrDefault(userAssignmentSolution => userAssignmentSolution.UserAssignmentSolutionId == CheckedSolutionId)!;

        PlagiarisedSolutionsData = dbContext.UserAssignmentSolutions
            .Include(userAssignmentSolution => userAssignmentSolution.Assignment)
            .Include(userAssignmentSolution => userAssignmentSolution.User)
            .Include(userAssignmentSolution => userAssignmentSolution.Plagiarism)
            .ThenInclude(plagiarism => plagiarism.PlagiarismEntries)
            .ToList();
        
        foreach (var solution in PlagiarisedSolutionsData)
        {
            dbContext.Entry(solution.Plagiarism)
                .Collection(plagiarism => plagiarism.PlagiarismEntries)
                .Load();
        }

        PlagiarisedSolutionsData = PlagiarisedSolutionsData
            .Where(userAssignmentSolution => PlagiarizedIds.Contains(userAssignmentSolution.UserAssignmentSolutionId))
            .ToList();

        var userIds = PlagiarisedSolutionsData.Select(solution => solution.UserId).Distinct().ToList();
        userIds.Add(CheckedSolutionData.UserId);

        UsersData = dbContext.Users
            .Where(user => userIds.Contains(user.UserId))
            .ToList();
    }

}