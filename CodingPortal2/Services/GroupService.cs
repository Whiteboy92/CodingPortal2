using CodingPortal2.Database;
using CodingPortal2.DbModels;
using Microsoft.EntityFrameworkCore;
namespace CodingPortal2.Services;

public class GroupService
{
    private readonly ApplicationDbContext dbContext;
    
    public GroupService(ApplicationDbContext dbContext)
    {
        this.dbContext = dbContext;
    }
    
    public async Task<List<Group>> GetCreatorGroupsAsync(int userId)
    {
        return await dbContext.Groups
            .Where(group => group.CreatorUserId == userId)
            .ToListAsync();
    }

}