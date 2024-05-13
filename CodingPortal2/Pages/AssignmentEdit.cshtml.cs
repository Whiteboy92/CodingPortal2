using CodingPortal2.Database;
using CodingPortal2.DbModels;
using CodingPortal2.Services;
using CodingPortal2.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
namespace CodingPortal2.Pages;

public class AssignmentEditModel : PageModel
{
    private readonly ApplicationDbContext dbContext;
    private readonly IMemoryCache memoryCache;
    private readonly AssignmentService assignmentService;
    private readonly string loggedAsUserLoginCacheKey = "LoggedAsUserLogin";
    
    public AssignmentEditModel(ApplicationDbContext dbContext, IMemoryCache memoryCache, AssignmentService assignmentService)
    {
        this.dbContext = dbContext;
        this.memoryCache = memoryCache;
        this.assignmentService = assignmentService;
    }
    
    [BindProperty(SupportsGet = true)]
    public Assignment Assignment { get; set; }

    public string LoggedAsLogin = "";
    public string PermissionLevel { get; private set; }
    public int AssignmentId { get; set; }
    public string Login { get; set; } = "";
    public int UserId { get; set; }

    public IActionResult OnGet(int assignmentId)
    {
        (PermissionLevel, Login, UserId) = UserHelper.GetUserProperties(HttpContext);
        PageHelper.GetTempDataMessagesAndSetToViewData(TempData, ViewData);

        if(UserHelper.GetLoggedAsUserLogin(memoryCache, loggedAsUserLoginCacheKey) != "")
        {
            LoggedAsLogin = UserHelper.GetLoggedAsUserLogin(memoryCache, loggedAsUserLoginCacheKey);
            UserId = dbContext.Users.FirstOrDefault(user => user.Login == LoggedAsLogin)?.UserId ?? 0;
        }
        
        Assignment = assignmentService.GetAssignmentById(assignmentId) ?? new Assignment();
        
        return Page();
    }
    
    public async Task<IActionResult> OnPostAsync(string action)
    {
        (PermissionLevel, Login, UserId) = UserHelper.GetUserProperties(HttpContext);

        return action switch
        {
            "SaveChanges" => await OnPostUpdateAssignment(Assignment.AssignmentId),
            _ => RedirectToPage("/AssignmentEdit")
        };
    }

    private async Task<IActionResult> OnPostUpdateAssignment(int assignmentId)
    {
        Assignment existingAssignment = await assignmentService.GetAssignmentByIdAsync(assignmentId) ?? throw new InvalidOperationException();
        
        int totalDays = ParseAndValidateInput(Request.Form["Days"]);
        int totalMinutes = ParseAndValidateInput(Request.Form["Minutes"]);
        int totalHours = ParseAndValidateInput(Request.Form["Hours"]);

        string pathToTestsFolder = Request.Form["PathToTestsFolder"].ToString()?.Trim() ?? string.Empty;
        TimeSpan uploadFrequency = TimeHelper.CalculateTimeSpanDaysHoursMinutes(totalDays, totalHours, totalMinutes);
        
        if ((totalHours > 0 || totalMinutes > 0) && pathToTestsFolder != string.Empty)
        {
            existingAssignment.Title = Assignment.Title;
            existingAssignment.Content = Assignment.Content;
            existingAssignment.TotalPoints = Assignment.TotalPoints;
            existingAssignment.ProgrammingLanguage = Assignment.ProgrammingLanguage;
            existingAssignment.UploadFrequency = uploadFrequency;
            existingAssignment.PathToTests = pathToTestsFolder;

            dbContext.Assignments.Update(existingAssignment);
            await dbContext.SaveChangesAsync();
        
            PageHelper.SetTempDataSuccessMessage($"Task {existingAssignment.Title} updated.", TempData);
            return RedirectToPage("/Index");
        }
        
        PageHelper.SetTempDataErrorMessage($"Error updating task. Please check your input.", TempData);
        return RedirectToPage("/AssignmentEdit");
    }
    
    public JsonResult OnGetCheckTitleExists(string title, int assignmentId)
    {
        if (string.IsNullOrEmpty(title)) { return new JsonResult(new { exists = false }); }
        
        bool titleExists = dbContext.Assignments.Any(assignment => assignment.Title == title && assignment.AssignmentId != assignmentId);

        return new JsonResult(new { exists = titleExists });
    }
    
    protected virtual int ParseAndValidateInput(StringValues input)
    {
        if (int.TryParse(input.ToString(), out int result))
        {
            return result;
        }
        return 0;
    }
}