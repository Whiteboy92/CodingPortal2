using CodingPortal2.Database;
using CodingPortal2.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
namespace CodingPortal2.Pages
{
    public class LogoutAsOtherUserModel : PageModel
    {
        private readonly ApplicationDbContext dbContext;
        private readonly IMemoryCache memoryCache;
        private readonly string loggedAsUserLoginCacheKey = "LoggedAsUserLogin";
        
        public LogoutAsOtherUserModel(ApplicationDbContext dbContext, IMemoryCache memoryCache)
        {
            this.dbContext = dbContext;
            this.memoryCache = memoryCache;
        }
        
        public string? PermissionLevel { get; private set; }
        public string Login { get; set; } = "";
        public string LoggedAsLogin = "";
        public int UserId { get; set; }
        
        public RedirectToPageResult OnGet()
        {
            (PermissionLevel, Login, UserId) = UserHelper.GetUserProperties(HttpContext);
            PageHelper.GetTempDataMessagesAndSetToViewData(TempData, ViewData);

            if (UserHelper.GetLoggedAsUserLogin(memoryCache, loggedAsUserLoginCacheKey) != "")
            {
                LoggedAsLogin = UserHelper.GetLoggedAsUserLogin(memoryCache, loggedAsUserLoginCacheKey);
                UserId = dbContext.Users.FirstOrDefault(user => user.Login == LoggedAsLogin)?.UserId ?? 0;
            }
    
            ClearCacheToLogout();
            return RedirectToPage("/Index");
        }
        
        private void ClearCacheToLogout()
        {
            memoryCache.Remove(loggedAsUserLoginCacheKey);
        }
    }
}