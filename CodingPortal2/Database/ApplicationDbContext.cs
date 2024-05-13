using CodingPortal2.DbModels;
using Microsoft.EntityFrameworkCore;
namespace CodingPortal2.Database;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Group> Groups { get; set; }
    public DbSet<Plagiarism> Plagiarisms { get; set; }
    public DbSet<Assignment> Assignments { get; set; }
    public DbSet<UserAssignmentDate> UserAssignmentDates { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<UserGroup> UserGroups { get; set; }
    public DbSet<UserAssignmentSolution> UserAssignmentSolutions { get; set; }
    public DbSet<PlagiarismEntry> PlagiarismEntries { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserGroup>()
            .HasKey(userGroup => new { userGroup.UserId, userGroup.GroupId });
        
        modelBuilder.Entity<UserAssignmentSolution>()
            .HasOne(userAssignmentSolution => userAssignmentSolution.Assignment)
            .WithMany(assignment => assignment.UserSolutions)
            .HasForeignKey(userAssignmentSolution => userAssignmentSolution.AssignmentId);
        
        modelBuilder.Entity<UserAssignmentSolution>()
            .HasOne(userAssignmentSolution => userAssignmentSolution.Plagiarism)
            .WithOne(plagiarism => plagiarism.UserAssignmentSolution)
            .HasForeignKey<Plagiarism>(plagiarism => plagiarism.UserSolutionId)
            .OnDelete(DeleteBehavior.Restrict);
        
        modelBuilder.Entity<UserAssignmentDate>()
            .HasKey(userAssignmentDate => new { userAssignmentDate.UserId, userAssignmentDate.AssignmentId });

        modelBuilder.Entity<UserAssignmentDate>()
            .HasOne(userAssignmentDate => userAssignmentDate.User)
            .WithMany(user => user.AssignedTasks)
            .HasForeignKey(userAssignmentDate => userAssignmentDate.UserId);

        modelBuilder.Entity<UserAssignmentDate>()
            .HasOne(userAssignmentDate => userAssignmentDate.Assignment)
            .WithMany(assignment => assignment.AssignedUsers)
            .HasForeignKey(userAssignmentDate => userAssignmentDate.AssignmentId);

        modelBuilder.Entity<UserGroup>()
            .HasOne(userGroup => userGroup.User)
            .WithMany(user => user.UserGroups)
            .HasForeignKey(userGroup => userGroup.UserId);

        modelBuilder.Entity<UserGroup>()
            .HasOne(userGroup => userGroup.Group)
            .WithMany(group => group.UserGroups)
            .HasForeignKey(userGroup => userGroup.GroupId);

        modelBuilder.Entity<Group>()
            .HasOne(group => group.Creator)
            .WithMany(user => user.CreatedGroups)
            .HasForeignKey(group => group.CreatorUserId)
            .OnDelete(DeleteBehavior.Restrict);
        
        modelBuilder.Entity<Plagiarism>()
            .HasKey(plagiarism => plagiarism.PlagiarismId);

        modelBuilder.Entity<Plagiarism>()
            .HasOne(plagiarism => plagiarism.UserAssignmentSolution)
            .WithOne(userAssignmentSolution => userAssignmentSolution.Plagiarism)
            .HasForeignKey<UserAssignmentSolution>(userAssignmentSolution => userAssignmentSolution.PlagiarismId);

        modelBuilder.Entity<PlagiarismEntry>()
            .HasKey(entry => entry.PlagiarismEntryId);

        modelBuilder.Entity<PlagiarismEntry>()
            .HasOne(entry => entry.PlagiarisedSolution)
            .WithMany()
            .HasForeignKey(entry => entry.PlagiarisedSolutionId)
            .OnDelete(DeleteBehavior.Restrict);


        base.OnModelCreating(modelBuilder);
    }

}