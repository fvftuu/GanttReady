using Microsoft.EntityFrameworkCore;
using NetPlan.Server.Models;

namespace NetPlan.Server.Data;

public class NetPlanDbContext : DbContext
{
    public NetPlanDbContext(DbContextOptions<NetPlanDbContext> options)
        : base(options)
    {
    }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<TaskRelation> TaskRelations => Set<TaskRelation>();
    public DbSet<Resource> Resources => Set<Resource>();
    public DbSet<ResourceAssignment> ResourceAssignments => Set<ResourceAssignment>();
    public DbSet<ColumnDefinition> ColumnDefinitions => Set<ColumnDefinition>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Project 配置
        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasIndex(p => p.Code).IsUnique();
        });

        // TaskItem 配置
        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.HasOne(t => t.Project)
                .WithMany(p => p.Tasks)
                .HasForeignKey(t => t.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(t => t.ParentTask)
                .WithMany(t => t.SubTasks)
                .HasForeignKey(t => t.ParentTaskId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(t => new { t.ProjectId, t.SortOrder });
        });

        // TaskRelation 配置
        modelBuilder.Entity<TaskRelation>(entity =>
        {
            entity.HasOne(r => r.Project)
                .WithMany()
                .HasForeignKey(r => r.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.PredecessorTask)
                .WithMany(t => t.Successors)
                .HasForeignKey(r => r.PredecessorTaskId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.SuccessorTask)
                .WithMany(t => t.Predecessors)
                .HasForeignKey(r => r.SuccessorTaskId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(r => r.ProjectId);
        });

        // Resource 配置
        modelBuilder.Entity<Resource>(entity =>
        {
            entity.HasOne(r => r.Project)
                .WithMany(p => p.Resources)
                .HasForeignKey(r => r.ProjectId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ResourceAssignment 配置
        modelBuilder.Entity<ResourceAssignment>(entity =>
        {
            entity.HasOne(a => a.Task)
                .WithMany(t => t.ResourceAssignments)
                .HasForeignKey(a => a.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.Resource)
                .WithMany(r => r.Assignments)
                .HasForeignKey(a => a.ResourceId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ColumnDefinition 配置
        modelBuilder.Entity<ColumnDefinition>(entity =>
        {
            entity.HasOne(c => c.Project)
                .WithMany(p => p.ColumnDefinitions)
                .HasForeignKey(c => c.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(c => new { c.ProjectId, c.ViewName, c.SortOrder });
        });
    }
}
