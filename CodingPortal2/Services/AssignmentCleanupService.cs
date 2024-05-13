using CodingPortal2.Database;
using Microsoft.EntityFrameworkCore;
namespace CodingPortal2.Services;

public class AssignmentCleanupService : BackgroundService
{
    private readonly ApplicationDbContext dbContext;

    public AssignmentCleanupService(ApplicationDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            await DeAssignOverdueAssignments();
        }
    }

    private async Task DeAssignOverdueAssignments()
    {
        var overdueAssignments = dbContext.UserAssignmentDates
            .Include(userAssignmentDate => userAssignmentDate.Assignment)
            .Where(userAssignmentDate => userAssignmentDate.DeadLineDateTime < DateTimeOffset.Now && !userAssignmentDate.Assignment.IsConfirmed)
            .ToList();

        foreach (var overdueAssignment in overdueAssignments)
        {
            var userAssignmentDatesToRemove = dbContext.UserAssignmentDates
                .Where(userAssignmentDate => userAssignmentDate.AssignmentId == overdueAssignment.AssignmentId && userAssignmentDate.UserId == overdueAssignment.UserId)
                .ToList();

            dbContext.UserAssignmentDates.RemoveRange(userAssignmentDatesToRemove);
            await dbContext.SaveChangesAsync();
        }
    }


}