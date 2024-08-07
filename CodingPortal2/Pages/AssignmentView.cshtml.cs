﻿using System.Diagnostics.CodeAnalysis;
using CodingPortal2.Database;
using CodingPortal2.DbModels;
using CodingPortal2.DockerComponents;
using CodingPortal2.Services;
using CodingPortal2.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
namespace CodingPortal2.Pages
{
    public class AssignmentViewModel : PageModel
    {
        private readonly ApplicationDbContext dbContext;
        private readonly IMemoryCache memoryCache;
        private readonly AssignmentService assignmentService;
        private readonly string loggedAsUserLoginCacheKey = "LoggedAsUserLogin";
        
        public AssignmentViewModel(ApplicationDbContext dbContext, IMemoryCache memoryCache,
            AssignmentService assignmentService, IConfiguration configuration)
        {
            this.dbContext = dbContext;
            this.memoryCache = memoryCache;
            this.assignmentService = assignmentService;
        }
        
        public string LoggedAsLogin = "";
        public string PermissionLevel { get; private set; }
        public string Login { get; set; } = "";
        public int UserId { get; set; }
        public Assignment? Assignment { get; set; }
        public TimeSpan RemainingTimeToDoAssignment { get; set; }
        public TimeSpan TimeToNextUpload { get; set; }
        
        //delete for release
        public int DecreaseTimeForTesting { get; set; } = 6;

        public IActionResult OnGet(int assignmentId)
        {
            (PermissionLevel, Login, UserId) = UserHelper.GetUserProperties(HttpContext);
            PageHelper.GetTempDataMessagesAndSetToViewData(TempData, ViewData);
            if (UserHelper.GetLoggedAsUserLogin(memoryCache, loggedAsUserLoginCacheKey) != "")
            {
                LoggedAsLogin = UserHelper.GetLoggedAsUserLogin(memoryCache, loggedAsUserLoginCacheKey);
                UserId = dbContext.Users.FirstOrDefault(user => user.Login == LoggedAsLogin)?.UserId ?? 0;
            }

            Assignment = assignmentService.GetAssignmentById(assignmentId) ?? new Assignment();

            GetUserTaskRemainingTime(assignmentId);

            return Page();
        }
        
        public async Task<IActionResult> OnPostAsync(int taskId, string action)
        {
            (PermissionLevel, Login, UserId) = UserHelper.GetUserProperties(HttpContext);

            return action switch
            {
                "SubmitCode" => await OnPostSubmitCode(taskId),
                _ => RedirectToPage("/TaskView")
            };
        }
        
        [SuppressMessage("ReSharper.DPA", "DPA0012: High execution time of Razor page handler", MessageId = "time: 17682ms")]
        public async Task<IActionResult> OnPostSubmitCode(int assignmentId)
        {
            (PermissionLevel, Login, UserId) = UserHelper.GetUserProperties(HttpContext);
            var assignment = await assignmentService.GetAssignmentByIdAsync(assignmentId);
            if (assignment == null) return NotFound();
            var userCode = Request.Form["code"].ToString()!;
            var userCodeNoFormat = Request.Form["codeWithoutHtmlFormat"].ToString()!;
            
            await SetUploadTimer(assignmentId);
            GetUserTaskRemainingTime(assignmentId);
            var language = assignment.ProgrammingLanguage;
            
            try
            {
                var dockerInitializer = new DockerComponentInitializer(dbContext);
                var result = await dockerInitializer.CompileUserCodeInContainer(userCode, userCodeNoFormat, language, assignmentId, UserId);
                

                if (result == "Compilation failed")
                {
                    PageHelper.SetTempDataErrorMessage($"Code sent successfully: {result}", TempData);
                }
                else
                {
                    PageHelper.SetTempDataSuccessMessage($"Code sent successfully: {result}", TempData);
                }
                
                return RedirectToPage("/Index");
            }
            catch (Exception ex)
            {
                PageHelper.SetTempDataErrorMessage($"Code compilation failed 1: {ex.Message}", TempData);
            }
            
            return RedirectToPage("/Index");
        }
        
        private async Task<UserAssignmentDate?> SetUploadTimer(int assignmentId)
        {
            (PermissionLevel, Login, UserId) = UserHelper.GetUserProperties(HttpContext);

            var userAssignmentDate = await dbContext.UserAssignmentDates
                .Include(userAssignmentDate => userAssignmentDate.User)
                .Include(userAssignmentDate => userAssignmentDate.Assignment)
                .Where(userAssignmentDate => userAssignmentDate.UserId == UserId && userAssignmentDate.AssignmentId == assignmentId)
                .FirstOrDefaultAsync();

            if (userAssignmentDate != null)
            {
                userAssignmentDate.LastUploadDateTime = DateTimeOffset.Now;
                await dbContext.SaveChangesAsync();
            }
            return userAssignmentDate;
        }
        
        private void GetUserTaskRemainingTime(int assignmentId)
        {
            var userAssignmentDate = dbContext.UserAssignmentDates.FirstOrDefault(userAssignmentDate => userAssignmentDate.AssignmentId == assignmentId && userAssignmentDate.UserId == UserId);
            if (userAssignmentDate == null || Assignment == null) return;
                
            RemainingTimeToDoAssignment = userAssignmentDate.DeadLineDateTime - DateTimeOffset.Now;
            if (userAssignmentDate.LastUploadDateTime == DateTimeOffset.MinValue)
            {
                TimeToNextUpload = TimeSpan.Zero;
            }
            else
            {
                TimeToNextUpload = userAssignmentDate.LastUploadDateTime + Assignment.UploadFrequency / DecreaseTimeForTesting - DateTimeOffset.Now;
            }
        }

        public JsonResult OnGetAssignmentTimeToNextUpload(int assignmentId)
        {
            var userAssignmentDate = dbContext.UserAssignmentDates.FirstOrDefault(userAssignmentDate => userAssignmentDate.AssignmentId == assignmentId && userAssignmentDate.UserId == UserId);
            if (userAssignmentDate == null)
            {
                return new JsonResult(new
                {
                    timeToNextUpload = TimeSpan.Zero
                });
            }

            TimeSpan timeToNextUpload = userAssignmentDate.TimeToNextUpload;
            return new JsonResult(new
            {
                timeToNextUpload
            });
        }
    }
}