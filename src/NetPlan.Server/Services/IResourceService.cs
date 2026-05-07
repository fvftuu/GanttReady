using NetPlan.Server.Models;

namespace NetPlan.Server.Services;

public interface IResourceService
{
    Task<List<Resource>> GetResourcesByProjectAsync(int projectId);
    Task<List<Resource>> GetAllResourcesAsync(); // 获取所有资源（含共享）
    Task<Resource?> GetResourceByIdAsync(int id);
    Task<Resource> CreateResourceAsync(Resource resource);
    Task<Resource> UpdateResourceAsync(Resource resource);
    Task DeleteResourceAsync(int id);
    Task<List<ResourceAssignment>> GetAssignmentsByTaskIdAsync(int taskId);
    Task<List<ResourceAssignment>> GetAssignmentsByProjectAsync(int projectId);
    /// <summary>
    /// 获取所有项目的所有资源分配（跨项目分析用）
    /// </summary>
    Task<List<ResourceAssignment>> GetAllAssignmentsAsync();
    Task<ResourceAssignment> CreateAssignmentAsync(ResourceAssignment assignment);
    Task<ResourceAssignment> UpdateAssignmentAsync(ResourceAssignment assignment);
    Task DeleteAssignmentAsync(int id);
    
    // 批量导入导出
    Task<byte[]> ExportResourcesToExcelAsync(int? projectId = null);
    Task<int> ImportResourcesFromExcelAsync(int? projectId, byte[] fileData);
}
