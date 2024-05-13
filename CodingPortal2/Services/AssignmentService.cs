using CodingPortal2.Database;
using CodingPortal2.DbModels;
namespace CodingPortal2.Services
{
    public class AssignmentService
    {
        private readonly ApplicationDbContext dbContext;

        public AssignmentService(ApplicationDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<bool> ToggleTaskConfirmationStatusAsync(int assignmentId)
        {
            var assignment = await dbContext.Assignments.FindAsync(assignmentId);
            if (assignment == null) { return false; }

            assignment.IsConfirmed = !assignment.IsConfirmed;
            await dbContext.SaveChangesAsync();

            return assignment.IsConfirmed;
        }

        public Assignment? GetAssignmentById(int assignmentId)
        {
            return assignmentId == 0 ? null : dbContext.Assignments.FirstOrDefault(assignment => assignment.AssignmentId == assignmentId);
        }
        
        public async Task<Assignment?> GetAssignmentByIdAsync(int assignmentId)
        {
            return await dbContext.Assignments.FindAsync(assignmentId);
        }
        
    }
}