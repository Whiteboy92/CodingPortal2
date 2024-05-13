using CodingPortal2.Database;
using CodingPortal2.DatabaseEnums;
using CodingPortal2.DbModels;
using CodingPortal2.Services;
using CodingPortal2.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
namespace CodingPortal2.Pages;

public class AssignmentCreationModel : PageModel
{
    private readonly ApplicationDbContext dbContext;
    private readonly IMemoryCache memoryCache;
    private readonly AssignmentService assignmentService;
    private readonly string loggedAsUserLoginCacheKey = "LoggedAsUserLogin";

    public AssignmentCreationModel(ApplicationDbContext dbContext, IMemoryCache memoryCache, AssignmentService assignmentService)
    {
        this.dbContext = dbContext;
        this.memoryCache = memoryCache;
        this.assignmentService = assignmentService;
    }

    [BindProperty]
    public Assignment Assignment { get; set; }

    public string LoggedAsLogin = "";
    public string PermissionLevel { get; private set; }
    public string Login { get; set; } = "";
    public int UserId { get; set; }

    public IActionResult OnGet(int assignmentId, string title, string content, int totalPoints, TimeSpan uploadFrequency, string pathToTests, ProgrammingLanguage programmingLanguage)
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
            "CreateAssignment" => await OnPostCreateAssignment(),
            _ => RedirectToPage("/AssignmentCreation")
        };
    }
        
    private async Task<IActionResult> OnPostCreateAssignment()
    {
        Login = HttpContext.Session.GetString("Login") ?? string.Empty;
        User creator = dbContext.Users.FirstOrDefault(user => user.Login == Login) ?? throw new InvalidOperationException();
            
        int totalMinutes = ParseAndValidateInput(Request.Form["Minutes"]);
        int totalHours = ParseAndValidateInput(Request.Form["Hours"]);

        string pathToTestsFolder = Request.Form["PathToTests"].ToString()?.Trim() ?? string.Empty;

        if ((totalHours > 0 || totalMinutes > 0) && pathToTestsFolder != string.Empty)
        {
            TimeSpan uploadFrequency = TimeHelper.CalculateTimeSpanHoursMinutes(totalHours, totalMinutes);
                
            Assignment = CreateNewAssignment(creator, uploadFrequency, pathToTestsFolder);

            dbContext.Assignments.Add(Assignment);
            await dbContext.SaveChangesAsync();

            await SetAssignmentCreator(Assignment, creator);

            PageHelper.SetTempDataSuccessMessage($"Assignment {Assignment.Title} created.", TempData);
            return RedirectToPage("/Index");
        }

        PageHelper.SetTempDataErrorMessage($"Assignment {Assignment.Title} not created.", TempData);
        return RedirectToPage("/AssignmentCreation");
    }
        
    private Assignment CreateNewAssignment(User creator, TimeSpan uploadFrequency, string pathToTests)
    {
        var newAssignment = new Assignment
        {
            TotalPoints = Assignment.TotalPoints,
            Title = Assignment.Title.Trim(),
            Content = Assignment.Content,
            UploadFrequency = uploadFrequency,
            IsConfirmed = false,
            ProgrammingLanguage = Assignment.ProgrammingLanguage,
            Creator = creator,
            PathToTests = pathToTests,
        };

        return newAssignment;
    }
        
    private async Task SetAssignmentCreator(Assignment assignment, User user)
    {
        var userAssignmentDate = new UserAssignmentDate
        {
            UserId = user.UserId,
            AssignmentId = assignment.AssignmentId,
            AssignmentTime = DateTimeOffset.Now,
            LastUploadDateTime = DateTimeOffset.MinValue,
        };

        dbContext.UserAssignmentDates.Add(userAssignmentDate);
        await dbContext.SaveChangesAsync();
        
        userAssignmentDate.DeadLineDateTime = userAssignmentDate.AssignmentTime.AddYears(99);
        
        dbContext.UserAssignmentDates.Update(userAssignmentDate);
        await dbContext.SaveChangesAsync();
    }
        
    public JsonResult OnGetCheckTitleExists(string title)
    {
        return dbContext.Assignments.Any(assignment => assignment.Title == title) 
            ? new JsonResult(new { exists = true }) 
            : new JsonResult(new { exists = false });
    }
        
    protected virtual int ParseAndValidateInput(StringValues input)
    {
        return int.TryParse(input.ToString(), out int result) ? result : 0;
    }
}


    