using Microsoft.EntityFrameworkCore;
using GanttReady.Server.Data;
using GanttReady.Server.Models;
using ClosedXML.Excel;

namespace GanttReady.Server.Services;

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
            .Where(r => r.ProjectId == null || r.ProjectId == projectId)
            .OrderBy(r => r.Code)
            .ToListAsync();
    }

    public async Task<List<Resource>> GetAllResourcesAsync()
    {
        return await _db.Resources
            .Include(r => r.Assignments)
            .OrderBy(r => r.Type)
            .ThenBy(r => r.Code)
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
        existing.ProjectId = resource.ProjectId; // 支持修改共享/项目归属

        await _db.SaveChangesAsync();
        return existing;
    }

    public async Task DeleteResourceAsync(int id)
    {
        var resource = await _db.Resources
            .Include(r => r.Assignments)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (resource == null) return;

        // 先删除关联的资源分配
        _db.ResourceAssignments.RemoveRange(resource.Assignments);
        _db.Resources.Remove(resource);
        await _db.SaveChangesAsync();
    }

    public async Task<List<ResourceAssignment>> GetAssignmentsByTaskIdAsync(int taskId)
    {
        return await _db.ResourceAssignments
            .Include(a => a.Resource)
            .Where(a => a.TaskId == taskId)
            .ToListAsync();
    }

    public async Task<List<ResourceAssignment>> GetAssignmentsByProjectAsync(int projectId)
    {
        return await _db.ResourceAssignments
            .Include(a => a.Resource)
            .Include(a => a.Task)
            .Where(a => a.Task.ProjectId == projectId)
            .ToListAsync();
    }

    /// <summary>
    /// 获取所有项目的所有资源分配（跨项目分析用）
    /// </summary>
    public async Task<List<ResourceAssignment>> GetAllAssignmentsAsync()
    {
        return await _db.ResourceAssignments
            .Include(a => a.Resource)
            .Include(a => a.Task)
            .ToListAsync();
    }

    public async Task<ResourceAssignment> CreateAssignmentAsync(ResourceAssignment assignment)
    {
        _db.ResourceAssignments.Add(assignment);
        await _db.SaveChangesAsync();
        return assignment;
    }

    public async Task<ResourceAssignment> UpdateAssignmentAsync(ResourceAssignment assignment)
    {
        var existing = await _db.ResourceAssignments.FindAsync(assignment.Id);
        if (existing != null)
        {
            existing.Quantity = assignment.Quantity;
            existing.Notes = assignment.Notes;
            await _db.SaveChangesAsync();
        }
        return existing ?? assignment;
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

    #region 批量导入导出

    /// <summary>
    /// 导出资源数据到Excel
    /// </summary>
    public async Task<byte[]> ExportResourcesToExcelAsync(int? projectId = null)
    {
        var query = _db.Resources.AsQueryable();
        if (projectId.HasValue)
        {
            query = query.Where(r => r.ProjectId == null || r.ProjectId == projectId.Value);
        }
        var resources = await query.OrderBy(r => r.Type).ThenBy(r => r.Code).ToListAsync();

        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var ws = workbook.Worksheets.Add("资源数据");

        var headers = new[] { "资源代码", "资源名称", "资源类型", "单位", "数量", "单价", "小时成本", "归属项目", "备注" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
            cell.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
            cell.Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
        }

        int row = 2;
        foreach (var r in resources)
        {
            var typeName = r.Type switch
            {
                ResourceType.Material => "Material",
                ResourceType.Equipment => "Equipment",
                ResourceType.Measure => "Measure",
                _ => "Labor"
            };

            ws.Cell(row, 1).Value = r.Code;
            ws.Cell(row, 2).Value = r.Name;
            ws.Cell(row, 3).Value = typeName;
            ws.Cell(row, 4).Value = r.Unit;
            ws.Cell(row, 5).Value = (double)r.Quantity;
            ws.Cell(row, 6).Value = (double)r.UnitPrice;
            ws.Cell(row, 7).Value = r.HourlyCost.HasValue ? (double)r.HourlyCost.Value : 0;
            ws.Cell(row, 8).Value = r.ProjectId.HasValue ? r.ProjectId.Value.ToString() : "共享";
            ws.Cell(row, 9).Value = r.Notes ?? "";
            row++;
        }

        ws.Column(1).Width = 12;
        ws.Column(2).Width = 20;
        ws.Column(3).Width = 12;
        ws.Column(4).Width = 8;
        ws.Column(5).Width = 8;
        ws.Column(6).Width = 10;
        ws.Column(7).Width = 12;
        ws.Column(8).Width = 12;
        ws.Column(9).Width = 25;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// 从Excel导入资源数据
    /// </summary>
    /// <returns>导入数量</returns>
    public async Task<int> ImportResourcesFromExcelAsync(int? projectId, byte[] fileData)
    {
        using var stream = new MemoryStream(fileData);
        using var workbook = new ClosedXML.Excel.XLWorkbook(stream);

        var ws = workbook.Worksheets.First();
        var rows = ws.RowsUsed().Skip(1).ToList();
        int importedCount = 0;

        var existingResources = await _db.Resources.ToListAsync();

        foreach (var row in rows)
        {
            try
            {
                var code = row.Cell(1).GetString().Trim();
                var name = row.Cell(2).GetString().Trim();

                if (string.IsNullOrEmpty(code) && string.IsNullOrEmpty(name))
                    continue;

                // 解析资源类型
                var typeStr = row.Cell(3).GetString().Trim();
                var type = typeStr switch
                {
                    "Material" => ResourceType.Material,
                    "Equipment" => ResourceType.Equipment,
                    "Measure" => ResourceType.Measure,
                    _ => ResourceType.Labor
                };

                // 解析归属项目
                var projectStr = row.Cell(8).GetString().Trim();
                int? targetProjectId = projectId;
                if (!string.IsNullOrEmpty(projectStr) && projectStr != "共享" && int.TryParse(projectStr, out var pid))
                {
                    targetProjectId = pid;
                }
                else if (projectStr == "共享" || string.IsNullOrEmpty(projectStr))
                {
                    targetProjectId = null;
                }

                var existing = existingResources.FirstOrDefault(r => r.Code == code && r.ProjectId == targetProjectId);
                if (existing != null)
                {
                    existing.Name = name ?? existing.Name;
                    existing.Type = type;
                    existing.Unit = row.Cell(4).GetString().Trim();
                    var qty = row.Cell(5).GetValue<decimal>();
                    if (qty > 0) existing.Quantity = qty;
                    if (!row.Cell(6).IsEmpty()) existing.UnitPrice = row.Cell(6).GetValue<decimal>();
                    existing.HourlyCost = row.Cell(7).GetValue<decimal?>();
                    existing.Notes = row.Cell(9).GetString().Trim();
                }
                else
                {
                    var resource = new Resource
                    {
                        ProjectId = targetProjectId,
                        Code = !string.IsNullOrEmpty(code) ? code : $"R-{Guid.NewGuid().ToString()[..6]}",
                        Name = name ?? code ?? "未命名",
                        Type = type,
                        Unit = row.Cell(4).GetString().Trim(),
                        Quantity = row.Cell(5).GetValue<decimal>(),
                        UnitPrice = row.Cell(6).GetValue<decimal>(),
                        HourlyCost = row.Cell(7).GetValue<decimal?>(),
                        Notes = row.Cell(9).GetString().Trim(),
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.Resources.Add(resource);
                }
                importedCount++;
            }
            catch
            {
                // 跳过有问题的行
            }
        }

        await _db.SaveChangesAsync();
        return importedCount;
    }

    #endregion
}
