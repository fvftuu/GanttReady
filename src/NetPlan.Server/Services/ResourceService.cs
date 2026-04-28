using Microsoft.EntityFrameworkCore;
using NetPlan.Server.Data;
using NetPlan.Server.Models;

namespace NetPlan.Server.Services;

public class ResourceService : IResourceService
{
    private readonly NetPlanDbContext _db;

    public ResourceService(NetPlanDbContext db)
    {
        _db = db;
    }

    public async Task<List<Resource>> GetResourcesByProjectAsync(int projectId)
    {
        return await _db.Resources
            .Include(r => r.Assignments)
            .Where(r => r.ProjectId == projectId)
            .OrderBy(r => r.Code)
            .ToListAsync();
    }

    public async Task<Resource?> GetResourceByIdAsync(int id)
    {
        return await _db.Resources
            .Include(r => r.Assignments)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<Resource> CreateResourceAsync(Resource resource)
    {
        _db.Resources.Add(resource);
        await _db.SaveChangesAsync();
        return resource;
    }

    public async Task<Resource> UpdateResourceAsync(Resource resource)
    {
        var existing = await _db.Resources.FindAsync(resource.Id);
        if (existing == null)
            throw new InvalidOperationException($"Resource {resource.Id} not found");

        existing.Code = resource.Code;
        existing.Name = resource.Name;
        existing.Type = resource.Type;
        existing.Unit = resource.Unit;
        existing.Quantity = resource.Quantity;
        existing.UnitPrice = resource.UnitPrice;
        existing.HourlyCost = resource.HourlyCost;
        existing.Notes = resource.Notes;
        existing.ExtraData = resource.ExtraData;

        await _db.SaveChangesAsync();
        return existing;
    }

    public async Task DeleteResourceAsync(int id)
    {
        var resource = await _db.Resources.FindAsync(id);
        if (resource != null)
        {
            _db.Resources.Remove(resource);
            await _db.SaveChangesAsync();
        }
    }

    public async Task<List<ResourceAssignment>> GetAssignmentsByTaskIdAsync(int taskId)
    {
        return await _db.ResourceAssignments
            .Include(a => a.Resource)
            .Where(a => a.TaskId == taskId)
            .ToListAsync();
    }

    public async Task<ResourceAssignment> CreateAssignmentAsync(ResourceAssignment assignment)
    {
        _db.ResourceAssignments.Add(assignment);
        await _db.SaveChangesAsync();
        return assignment;
    }

    public async Task DeleteAssignmentAsync(int id)
    {
        var assignment = await _db.ResourceAssignments.FindAsync(id);
        if (assignment != null)
        {
            _db.ResourceAssignments.Remove(assignment);
            await _db.SaveChangesAsync();
        }
    }
}
