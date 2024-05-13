using Microsoft.Extensions.Caching.Memory;
namespace CodingPortal2.Shared
{
    public static class UserHelper
    {
        public static (string PermissionLevel, string Login, int UserId) GetUserProperties(HttpContext httpContext)
        {
            var permissionLevel = httpContext.Session.GetString("PermissionLevel") ?? string.Empty;
            var login = httpContext.Session.GetString("Login") ?? string.Empty;
            var userId = httpContext.Session.GetInt32("UserId") ?? 0;

            return (permissionLevel, login, userId);
        }
        
        public static string GetLoggedAsUserLogin(IMemoryCache memoryCache, string loggedAsUserLoginCacheKey)
        {
            return memoryCache.TryGetValue(loggedAsUserLoginCacheKey, out string loggedAsLogin) ? loggedAsLogin : "";
        }
        
        public static string GetLayoutBasedOnPermissionLevel(string? permissionLevel) => permissionLevel switch
        {
            "Admin" => "~/Pages/Shared/_AdminLayout.cshtml",
            "Student" => "~/Pages/Shared/_StudentLayout.cshtml",
            "Teacher" => "~/Pages/Shared/_TeacherLayout.cshtml",
            _ => ""
        };
    }
}
