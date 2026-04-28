using NetPlan.Server.Models;

namespace NetPlan.Server.Services;

public interface IResourceService
{
    Task<List<Resource>> GetResourcesByProjectAsync(int projectId);
    Task<Resource?> GetResourceByIdAsync(int id);
    Task<Resource> CreateResourceAsync(Resource resource);
    Task<Resource> UpdateResourceAsync(Resource resource);
    Task DeleteResourceAsync(int id);
    Task<List<ResourceAssignment>> GetAssignmentsByTaskIdAsync(int taskId);
    Task<ResourceAssignment> CreateAssignmentAsync(ResourceAssignment assignment);
    Task DeleteAssignmentAsync(int id);
}
