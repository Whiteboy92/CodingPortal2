using System.Text;
using CodingPortal2.Database;
using CodingPortal2.DbModels;
using CodingPortal2.Services;
using CodingPortal2.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
namespace CodingPortal2.Pages;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext dbContext;
    private readonly IMemoryCache memoryCache;
    private readonly AssignmentService assignmentService;
    
    public IndexModel(ApplicationDbContext dbContext, IMemoryCache memoryCache, AssignmentService assignmentService)
    {
        this.dbContext = dbContext;
        this.memoryCache = memoryCache;
        this.assignmentService = assignmentService;
    }
                
    private readonly string loggedAsUserLoginCacheKey = "LoggedAsUserLogin";
    public List<Assignment> Assignments { get; set; }
    public int UserId;
    public string Login = "";
    public string LoggedAsLogin = "";
    public List<Group> UserGroups { get; set; }
    public string PermissionLevel = "";

    public IActionResult OnGet()
    {
        if (HttpContext.Session.GetString("PermissionLevel") != null)
        {
            (PermissionLevel, Login, UserId) = UserHelper.GetUserProperties(HttpContext);
            PageHelper.GetTempDataMessagesAndSetToViewData(TempData, ViewData);
            
            if(UserHelper.GetLoggedAsUserLogin(memoryCache, loggedAsUserLoginCacheKey) != "")
            {
                LoggedAsLogin = UserHelper.GetLoggedAsUserLogin(memoryCache, loggedAsUserLoginCacheKey);
                UserId = dbContext.Users.FirstOrDefault(user => user.Login == LoggedAsLogin)?.UserId ?? 0;
                Assignments = GetAssignmentListOfUserId(UserId);
                return Page();
            }

            Assignments =  GetAssignmentListOfUserId(UserId);
            return Page();
        }
        
        string authHeader = Request.Headers["Authorization"];
        
        if (authHeader != null && authHeader.StartsWith("Basic"))
        {
            var encodedUsernamePassword = authHeader.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)[1].Trim();
            var decodedUsernamePassword = Encoding.UTF8.GetString(Convert.FromBase64String(encodedUsernamePassword ?? throw new InvalidOperationException()));
            var userCredentials = decodedUsernamePassword.Split(':', 2);
            var login = userCredentials[0];

            // Check if the user is in database
            var user = dbContext.Users.FirstOrDefault(user => user.Login == login);
            
            if (user != null)
            {
                UserId = user.UserId;
                Login = login;
                PermissionLevel = user.PermissionLevel.ToString();
                Assignments = GetAssignmentListOfUserId(UserId);
                
                var permissionLevelFromDb = user.PermissionLevel.ToString();
                HttpContext.Session.SetString("PermissionLevel", permissionLevelFromDb);
                HttpContext.Session.SetString("Login", login);
                HttpContext.Session.SetInt32("UserId", UserId);
                
                return Page();
            }
        }
            
        // Send 401 if the user is not found or if there was an issue
        Response.Headers["WWW-Authenticate"] = "Basic realm=\"MyRealm\"";
        Response.StatusCode = 401;
        return new EmptyResult();
    }

    public async Task<IActionResult> OnPostAsync(string action, int assignmentId, bool isConfirmed)
    {
        (PermissionLevel, Login, UserId) = UserHelper.GetUserProperties(HttpContext);

        return action switch
        {
            "UpdateAssignmentStatus" => await OnPostUpdateAssignmentStatus(assignmentId),
            "DeleteAssignment" => await OnPostDeleteAssignment(assignmentId),
            "UseAsTemplate" => await OnPostUseAsTemplate(assignmentId),
            "AssignmentEdit" => await OnPostAssignmentEdit(assignmentId),
            _ => RedirectToPage("/Index")
        };
    }
    
    public async Task<IActionResult> OnPostAssignmentEdit(int assignmentId)
    {
        var assignment = await assignmentService.GetAssignmentByIdAsync(assignmentId);
        if (assignment == null)
        {
            PageHelper.SetTempDataErrorMessage($"Assignment with ID {assignmentId} not found.", TempData);
            return RedirectToPage("/Index");
        }

        PageHelper.SetTempDataSuccessMessage($"Assignment {assignment.AssignmentId} {assignment.Title} set for editing.", TempData);

        return await Task.FromResult<IActionResult>(RedirectToPage("/AssignmentEdit", new
        {
            assignment.AssignmentId,
        }));
    }

    public async Task<IActionResult> OnPostUseAsTemplate(int assignmentId)
    {
        var assignment = await assignmentService.GetAssignmentByIdAsync(assignmentId);
        if (assignment == null) { return await Task.FromResult<IActionResult>(NotFound()); }
    
        PageHelper.SetTempDataSuccessMessage($"Assignment {assignment.Title} used as a template.", TempData);
        
        return await Task.FromResult<IActionResult>(RedirectToPage("/AssignmentCreation", new
        {
            assignment.AssignmentId,
        }));
    }
    
    private List<Assignment> GetAssignmentListOfUserId(int userId)
    {
        return dbContext.UserAssignmentDates
            .Where(userAssignmentDate => userAssignmentDate.UserId == userId)
            .Select(userAssignmentDate => userAssignmentDate.Assignment)
            .Where(assignment =>
                (assignment.Creator.UserId == userId) ||
                (assignment.IsConfirmed && dbContext.UserAssignmentDates
                    .Any(userAssignmentDate => userAssignmentDate.AssignmentId == assignment.AssignmentId &&
                                userAssignmentDate.UserId == userId && userAssignmentDate.DeadLineDateTime > DateTimeOffset.Now))
            )
            .ToList();
    }
    
    public async Task<IActionResult> OnPostUpdateAssignmentStatus(int assignmentId)
    {
        bool isConfirmed = await assignmentService.ToggleTaskConfirmationStatusAsync(assignmentId);
        var assignment = await assignmentService.GetAssignmentByIdAsync(assignmentId);

        if (assignment == null)
        {
            PageHelper.SetTempDataErrorMessage("An error occurred while updating the assignment.", TempData);
            return RedirectToPage("/Index");
        }
        
        string actionMessage = isConfirmed ? "confirmed" : "unconfirmed";
        PageHelper.SetTempDataSuccessMessage($"Assignment: {assignment.Title} {actionMessage}.", TempData);

        return RedirectToPage("/Index");
    }
    
    public async Task<IActionResult> OnPostDeleteAssignment(int assignmentId)
    {
        var assignment = await assignmentService.GetAssignmentByIdAsync(assignmentId);
        if (assignment == null) { return NotFound(); }
        
        dbContext.Assignments.Remove(assignment);
        await dbContext.SaveChangesAsync();
        
        PageHelper.SetTempDataSuccessMessage($"Assignment {assignment.Title} deleted.", TempData);
        
        return RedirectToPage("/Index");
    }

    public bool AssignmentHasSolutions(int assignmentId)
    {
        return dbContext.UserAssignmentSolutions.Any(solution => solution.AssignmentId == assignmentId);
    }
}