using CodingPortal2.Database;
using CodingPortal2.Shared;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
namespace CodingPortal2.Pages;

public class ViewSolutionCode : PageModel
{
    private readonly ApplicationDbContext dbContext;
    private readonly IMemoryCache memoryCache;
    private readonly string loggedAsUserLoginCacheKey = "LoggedAsUserLogin";
    
    public ViewSolutionCode(ApplicationDbContext dbContext, IMemoryCache memoryCache)
    {
        this.dbContext = dbContext;
        this.memoryCache = memoryCache;
    }

    public string LoggedAsLogin = "";
    public string PermissionLevel { get; private set; }
    public string Login { get; set; } = "";
    public int UserId { get; set; }
    public string? UserCode { get; set; }


    public Task OnGetAsync()
    {
        (PermissionLevel, Login, UserId) = UserHelper.GetUserProperties(HttpContext);
        
        if (TempData.TryGetValue("ViewCode", out var viewCode))
        {
            UserCode = viewCode as string;
        }
            
        if(UserHelper.GetLoggedAsUserLogin(memoryCache, loggedAsUserLoginCacheKey) != "")
        {
            LoggedAsLogin = UserHelper.GetLoggedAsUserLogin(memoryCache, loggedAsUserLoginCacheKey);
            UserId = dbContext.Users.FirstOrDefault(user => user.Login == LoggedAsLogin)?.UserId ?? 0;
        }
        
        return Task.CompletedTask;
    }
}