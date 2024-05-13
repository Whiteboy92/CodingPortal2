using CodingPortal2.Database;
using CodingPortal2.DatabaseEnums;
using CodingPortal2.DbModels;
using CodingPortal2.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using OfficeOpenXml;
using Task = System.Threading.Tasks.Task;

namespace CodingPortal2.Pages
{
    public class AddUsersModel : PageModel
    {
        private readonly IMemoryCache memoryCache;
        private readonly ApplicationDbContext dbContext;
        private readonly string excelFileCacheKey = "ExcelFile";
        private readonly string loggedAsUserLoginCacheKey = "LoggedAsUserLogin";
        
        public AddUsersModel(IMemoryCache memoryCache, ApplicationDbContext dbContext)
        {
            this.dbContext = dbContext;
            this.memoryCache = memoryCache;
        }

        public string LoggedAsLogin = "";
        public string? PermissionLevel { get; private set; }
        public string Login { get; set; } = "";
        public List<string>? ExtractedData { get; set; }
        public List<Group> Groups { get; set; }
        public int UserId { get; set; }
        
        public void OnGetAsync()
        {
            // Existing code to retrieve user properties
            (PermissionLevel, Login, UserId) = UserHelper.GetUserProperties(HttpContext);
            PageHelper.GetTempDataMessagesAndSetToViewData(TempData, ViewData);

            ExtractedData = GetExtractedDataFromCache();

            // Filter groups based on the current user as the creator
            Groups = dbContext.Groups.Where(group => group.CreatorUserId == UserId).ToList();

            if (UserHelper.GetLoggedAsUserLogin(memoryCache, loggedAsUserLoginCacheKey) != "")
            {
                LoggedAsLogin = UserHelper.GetLoggedAsUserLogin(memoryCache, loggedAsUserLoginCacheKey);
                UserId = dbContext.Users.FirstOrDefault(user => user.Login == LoggedAsLogin)?.UserId ?? 0;
            }
        }


        public async Task<IActionResult> OnPostAsync(IFormFile? file, string action)
        {
            (PermissionLevel, Login, UserId) = UserHelper.GetUserProperties(HttpContext);

            return action switch
            {
                "UploadFile" => await OnPostUploadFileAsync(file),
                "SendUsersToDatabase" => await OnPostSendUsersToDatabaseAsync(),
                _ => RedirectToPage("/AddUsers")
            };
        }

        public async Task<IActionResult> OnPostUploadFileAsync(IFormFile? fileInput)
        {
            if (fileInput == null || fileInput.Length == 0)
            {
                PageHelper.SetTempDataErrorMessage("File not selected", TempData);
                return RedirectToPage("AddUsers");
            }

            await ProcessExcelFileAsync(fileInput);

            PageHelper.SetTempDataSuccessMessage("File uploaded successfully", TempData);
            return RedirectToPage("AddUsers");
        }

        private Task ProcessExcelFileAsync(IFormFile? fileInput)
        {
            using var stream = fileInput?.OpenReadStream();
            using var package = new ExcelPackage(stream);

            var worksheet = package.Workbook.Worksheets.FirstOrDefault();

            if (worksheet == null)
            {
                PageHelper.SetTempDataErrorMessage("Worksheet not found in the Excel file", TempData);
                return Task.CompletedTask;
            }

            // Find the column index with the header "Login"
            var loginColumnIndex = -1;
            for (int col = worksheet.Dimension.Start.Column; col <= worksheet.Dimension.End.Column; col++)
            {
                var columnHeader = worksheet.Cells[1, col].Text;
                if (columnHeader.Equals("Login", StringComparison.OrdinalIgnoreCase))
                {
                    loginColumnIndex = col;
                    break;
                }
            }

            if (loginColumnIndex == -1)
            {
                PageHelper.SetTempDataErrorMessage("Login column not found", TempData);
                return Task.CompletedTask;
            }

            var loginColumn = worksheet.Cells[2, loginColumnIndex, worksheet.Dimension.End.Row, loginColumnIndex];

            ExtractedData = loginColumn
                .Select(loginCell => (loginCell.Text))
                .Select(login => (login))
                .ToList();

            CacheExtractedData();
            return Task.CompletedTask;
        }
        
        public async Task<IActionResult> OnPostSendUsersToDatabaseAsync()
        {
            var loginTextArea = Request.Form["loginTextArea"].ToString()?.Trim();
            var groupCodeInput = Request.Form["groupCodeInput"].ToString()?.Trim();

            if (string.IsNullOrWhiteSpace(loginTextArea))
            {
                PageHelper.SetTempDataErrorMessage("No data to send to the database", TempData);
                return RedirectToPage("/AddUsers");
            }

            if (string.IsNullOrWhiteSpace(groupCodeInput))
            {
                PageHelper.SetTempDataErrorMessage("Group code cannot be empty", TempData);
                return RedirectToPage("/AddUsers");
            }
            var extractedData = loginTextArea.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();

            if (extractedData.Count == 0)
            {
                PageHelper.SetTempDataErrorMessage("No valid logins found in the textarea", TempData);
                return RedirectToPage("/AddUsers");
            }
            
            await AddGroupsAndUsersToDbAsync(extractedData);
            RemoveCachedExtractedData();
            PageHelper.SetTempDataSuccessMessage("Users sent to the database successfully", TempData);
            return RedirectToPage("/UserList");
        }
        
        private async Task AddGroupsAndUsersToDbAsync(List<string> logins)
        {
            await using var separateDbContext = GetSeparateDbContext();
            
            var groupCodeInput = Request.Form["groupCodeInput"].ToString();
            if(groupCodeInput == null) return;
            
            await CreateGroupIfNotExisting(separateDbContext, groupCodeInput);
            await CreateUserIfNotExisting(logins, separateDbContext, groupCodeInput);
        }
        private async Task CreateGroupIfNotExisting(ApplicationDbContext separateDbContext, string groupCodeInput)
        {

            var existingGroup = await separateDbContext.Groups.FirstOrDefaultAsync(group1 => group1.Code == groupCodeInput);
            if (existingGroup == null)
            {
                if (!string.IsNullOrEmpty(groupCodeInput))
                {
                    var newGroup = new Group
                    {
                        Code = groupCodeInput,
                        Year = (Request.Form["yearSelect"]),
                        Semester = Enum.Parse<Semester>(Request.Form["semesterSelect"]),
                        CreatorUserId = UserId,
                    };

                    separateDbContext.Groups.Add(newGroup);
                    await separateDbContext.SaveChangesAsync();
                }
            }
        }
        private async Task CreateUserIfNotExisting(List<string> logins, ApplicationDbContext separateDbContext, string groupCodeInput)
        {
            foreach (var login in logins)
            {
                var existingUser = await separateDbContext.Users.FirstOrDefaultAsync(user => user.Login == login);
                if (existingUser == null)
                {
                    var newUser = new User
                    {
                        Login = login,
                        PermissionLevel = DatabaseEnums.PermissionLevel.Student,
                    };

                    separateDbContext.Users.Add(newUser);
                    await separateDbContext.SaveChangesAsync();

                    await AssociateUserWithGroup(newUser.UserId, groupCodeInput);
                }
                else
                {
                    await AssociateUserWithGroup(existingUser.UserId, groupCodeInput);
                }
            }
        }

        private async Task AssociateUserWithGroup(int userId, string groupCode)
        {
            var separateDbContext = GetSeparateDbContext();
            var groupId = await separateDbContext.Groups.FirstOrDefaultAsync(group => group.Code == groupCode);

            if (groupId != null)
            {
                var userGroup = await separateDbContext.UserGroups.FirstOrDefaultAsync(userGroup1 => userGroup1.UserId == userId && userGroup1.GroupId == groupId.GroupId);

                if (userGroup == null)
                {
                    userGroup = new UserGroup
                    {
                        UserId = userId,
                        GroupId = groupId.GroupId,
                    };

                    separateDbContext.UserGroups.Add(userGroup);
                    await separateDbContext.SaveChangesAsync();
                }
            }
        }
        
        private ApplicationDbContext GetSeparateDbContext()
        {
            var dbContextOptions = HttpContext.RequestServices.GetRequiredService<DbContextOptions<ApplicationDbContext>>();
            return new ApplicationDbContext(dbContextOptions);
        }
        
        private void CacheExtractedData()
        {
            memoryCache.Set(excelFileCacheKey, ExtractedData, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });
        }
        
        private List<string>? GetExtractedDataFromCache()
        {
            return memoryCache.TryGetValue(excelFileCacheKey, out List<string>? extractedData) 
                ? extractedData 
                : null;
        }
        
        private void RemoveCachedExtractedData()
        {
            memoryCache.Remove(excelFileCacheKey);
        }
        
        public JsonResult OnGetGroupDetails(string groupCode)
        {
            var group = dbContext.Groups.FirstOrDefault(group1 => group1.Code == groupCode);

            if (group != null)
            {
                return new JsonResult(new { year = group.Year, semester = group.Semester });
            }

            return new JsonResult(new { year = "", semester = "" });
        }
        
        public JsonResult OnGetYears()
        {
            var currentYear = DateTimeOffset.Now.Year;
            var years = new List<string>();
            for (int i = currentYear - 5; i < currentYear + 5; i++)
            {
                years.Add($"{i}/{i + 1}");
            }

            return new JsonResult(years);
        }
    }
}
