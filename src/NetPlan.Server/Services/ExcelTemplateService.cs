using ClosedXML.Excel;
using NetPlan.Server.Data;
using NetPlan.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace NetPlan.Server.Services;

/// <summary>
/// Excel模板导入导出服务（适配用户"进度计划表.xlsx"格式）
/// </summary>
public class ExcelTemplateService
{
    private readonly NetPlanDbContext _context;
    private readonly ILogger<ExcelTemplateService> _logger;

    public ExcelTemplateService(NetPlanDbContext context, ILogger<ExcelTemplateService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region 模板导出

    /// <summary>
    /// 生成项目Excel模板（与用户提供的格式一致）
    /// </summary>
    public async Task<byte[]> GenerateTemplateAsync(int projectId)
    {
        var project = await _context.Projects
            .Include(p => p.Tasks).ThenInclude(t => t.ResourceAssignments).ThenInclude(a => a.Resource)
            .Include(p => p.Tasks).ThenInclude(t => t.Predecessors)
            .Include(p => p.Resources)
            .FirstOrDefaultAsync(p => p.Id == projectId);

        if (project == null)
            throw new ArgumentException($"项目不存在: {projectId}");

        using var workbook = new XLWorkbook();

        // ========== Sheet 1: 任务与资源数据 ==========
        var ws1 = workbook.Worksheets.Add("任务与资源数据");
        var headers1 = new[] { "序号", "任务代码", "任务名称", "负责人", "计划开始", "计划完成", "工期(天)", "实际开始", "实际完成", "前置任务", "时差", "关系类型", "完成率(%)", "资源名称", "资源数量", "单位", "备注" };

        for (int i = 0; i < headers1.Length; i++)
        {
            var cell = ws1.Cell(1, i + 1);
            cell.Value = headers1[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }

        var tasks = project.Tasks.OrderBy(t => t.SortOrder).ToList();
        int row = 2;

        foreach (var task in tasks)
        {
            // 获取前置任务
            var predecessors = string.Join(",", task.Predecessors.Select(p => p.PredecessorTask.Code));

            // 获取关系类型
            var relationType = "FS";
            if (task.Predecessors.Count > 0)
                relationType = task.Predecessors.First().Type switch
                {
                    RelationType.SS => "SS",
                    RelationType.FF => "FF",
                    RelationType.SF => "SF",
                    _ => "FS"
                };

            var lag = task.Predecessors.Count > 0 ? task.Predecessors.First().Lag : 0;

            // 如果任务没有资源分配，输出一行
            if (task.ResourceAssignments.Count == 0)
            {
                ws1.Cell(row, 1).Value = task.SortOrder;
                ws1.Cell(row, 2).Value = task.Code;
                ws1.Cell(row, 3).Value = task.Name;
                ws1.Cell(row, 4).Value = ""; // 负责人
                ws1.Cell(row, 5).Value = task.PlanStartDate.ToString("yyyy-MM-dd");
                ws1.Cell(row, 6).Value = task.PlanEndDate.ToString("yyyy-MM-dd");
                ws1.Cell(row, 7).Value = task.PlanDuration;
                ws1.Cell(row, 8).Value = ""; // 实际开始
                ws1.Cell(row, 9).Value = ""; // 实际完成
                ws1.Cell(row, 10).Value = predecessors;
                ws1.Cell(row, 11).Value = lag;
                ws1.Cell(row, 12).Value = relationType;
                ws1.Cell(row, 13).Value = 0; // 完成率
                ws1.Cell(row, 14).Value = ""; // 资源名称
                ws1.Cell(row, 15).Value = ""; // 资源数量
                ws1.Cell(row, 16).Value = ""; // 单位
                ws1.Cell(row, 17).Value = ""; // 备注
                row++;
            }
            else
            {
                // 有多个资源分配时，每个资源一行（任务信息重复）
                foreach (var assignment in task.ResourceAssignments)
                {
                    ws1.Cell(row, 1).Value = task.SortOrder;
                    ws1.Cell(row, 2).Value = task.Code;
                    ws1.Cell(row, 3).Value = task.Name;
                    ws1.Cell(row, 4).Value = ""; // 负责人
                    ws1.Cell(row, 5).Value = task.PlanStartDate.ToString("yyyy-MM-dd");
                    ws1.Cell(row, 6).Value = task.PlanEndDate.ToString("yyyy-MM-dd");
                    ws1.Cell(row, 7).Value = task.PlanDuration;
                    ws1.Cell(row, 8).Value = ""; // 实际开始
                    ws1.Cell(row, 9).Value = ""; // 实际完成
                    ws1.Cell(row, 10).Value = predecessors;
                    ws1.Cell(row, 11).Value = lag;
                    ws1.Cell(row, 12).Value = relationType;
                    ws1.Cell(row, 13).Value = 0; // 完成率
                    ws1.Cell(row, 14).Value = assignment.Resource?.Name ?? "";
                    ws1.Cell(row, 15).Value = (double)assignment.Quantity;
                    ws1.Cell(row, 16).Value = assignment.Resource?.Unit ?? "";
                    ws1.Cell(row, 17).Value = assignment.Notes ?? "";
                    row++;
                }
            }
        }

        // 列宽
        ws1.Column(1).Width = 6;
        ws1.Column(2).Width = 10;
        ws1.Column(3).Width = 22;
        ws1.Column(4).Width = 10;
        ws1.Column(5).Width = 14;
        ws1.Column(6).Width = 14;
        ws1.Column(7).Width = 10;
        ws1.Column(8).Width = 14;
        ws1.Column(9).Width = 14;
        ws1.Column(10).Width = 14;
        ws1.Column(11).Width = 6;
        ws1.Column(12).Width = 8;
        ws1.Column(13).Width = 10;
        ws1.Column(14).Width = 18;
        ws1.Column(15).Width = 10;
        ws1.Column(16).Width = 8;
        ws1.Column(17).Width = 15;

        // ========== Sheet 2: 资源数据 ==========
        var ws2 = workbook.Worksheets.Add("资源数据");
        var headers2 = new[] { "资源代码", "资源名称", "资源类型", "单位", "数量", "单价", "小时成本", "备注" };

        for (int i = 0; i < headers2.Length; i++)
        {
            var cell = ws2.Cell(1, i + 1);
            cell.Value = headers2[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }

        row = 2;
        foreach (var resource in project.Resources.OrderBy(r => r.Code))
        {
            var typeName = resource.Type switch
            {
                ResourceType.Material => "Material",
                ResourceType.Equipment => "Equipment",
                _ => "Labor"
            };

            ws2.Cell(row, 1).Value = resource.Code;
            ws2.Cell(row, 2).Value = resource.Name;
            ws2.Cell(row, 3).Value = typeName;
            ws2.Cell(row, 4).Value = resource.Unit;
            ws2.Cell(row, 5).Value = (double)resource.Quantity;
            ws2.Cell(row, 6).Value = (double)resource.UnitPrice;
            ws2.Cell(row, 7).Value = resource.HourlyCost.HasValue ? (double)resource.HourlyCost.Value : 0;
            ws2.Cell(row, 8).Value = resource.Notes ?? "";
            row++;
        }

        ws2.Column(1).Width = 10;
        ws2.Column(2).Width = 18;
        ws2.Column(3).Width = 12;
        ws2.Column(4).Width = 8;
        ws2.Column(5).Width = 8;
        ws2.Column(6).Width = 10;
        ws2.Column(7).Width = 12;
        ws2.Column(8).Width = 20;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    #endregion

    #region 数据导入

    /// <summary>
    /// 从Excel导入项目数据（适配用户格式）
    /// </summary>
    public async Task<ExcelImportResult> ImportFromExcelAsync(int projectId, byte[] fileData)
    {
        var result = new ExcelImportResult();

        try
        {
            using var stream = new MemoryStream(fileData);
            using var workbook = new XLWorkbook(stream);

            // 检查工作表
            if (!workbook.Worksheets.Contains("任务与资源数据"))
            {
                result.Errors.Add("缺少【任务与资源数据】工作表");
                result.Success = false;
                result.Message = "导入失败";
                return result;
            }

            // Step 1: 先解析资源数据（如果有资源数据表）
            if (workbook.Worksheets.Contains("资源数据"))
            {
                var resourceErrors = await ImportResourcesAsync(projectId, workbook.Worksheet("资源数据"));
                result.Errors.AddRange(resourceErrors);
            }

            // Step 2: 解析任务与资源分配数据
            var taskErrors = await ImportTasksWithAssignmentsAsync(projectId, workbook.Worksheet("任务与资源数据"));
            result.Errors.AddRange(taskErrors);

            await _context.SaveChangesAsync();

            result.Success = result.Errors.Count == 0;
            result.Message = result.Success ? "导入成功" : $"导入完成，但有{result.Errors.Count}个错误";
            
            var taskCount = await _context.Tasks.CountAsync(t => t.ProjectId == projectId);
            var resourceCount = await _context.Resources.CountAsync(r => r.ProjectId == projectId);
            result.TasksImported = taskCount;
            result.ResourcesImported = resourceCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excel导入失败");
            result.Success = false;
            result.Message = $"导入失败: {ex.Message}";
            result.Errors.Add(ex.ToString());
        }

        return result;
    }

    /// <summary>
    /// 导入任务与资源分配数据（任务与资源数据表）
    /// 格式：序号, 任务代码, 任务名称, 负责人, 计划开始, 计划完成, 工期(天), 实际开始, 实际完成, 前置任务, 时差, 关系类型, 完成率(%), 资源名称, 资源数量, 单位, 备注
    /// </summary>
    private async Task<List<string>> ImportTasksWithAssignmentsAsync(int projectId, IXLWorksheet worksheet)
    {
        var errors = new List<string>();
        var taskCodeMap = new Dictionary<string, TaskItem>();
        var processedCodes = new HashSet<string>();

        // 获取现有任务
        var existingTasks = await _context.Tasks
            .Where(t => t.ProjectId == projectId)
            .ToListAsync();

        var rows = worksheet.RowsUsed().Skip(1).ToList();

        // 第一遍：创建/更新所有任务
        foreach (var row in rows)
        {
            try
            {
                var code = row.Cell(2).GetString().Trim(); // B列: 任务代码
                var name = row.Cell(3).GetString().Trim(); // C列: 任务名称

                if (string.IsNullOrEmpty(code))
                {
                    // 跳过空行（可能是资源行延伸）
                    var resName = row.Cell(14).GetString().Trim();
                    var seq = row.Cell(1).GetString().Trim();
                    if (!string.IsNullOrEmpty(resName) && string.IsNullOrEmpty(seq))
                        continue;
                    continue;
                }

                // 检查是否存在
                var existing = existingTasks.FirstOrDefault(t => t.Code == code);
                if (existing != null)
                {
                    // 更新现有任务（保留原有数据，只覆盖关键字段）
                    existing.Name = name;
                    existing.SortOrder = row.Cell(1).GetValue<int>();
                    
                    var startStr = row.Cell(5).GetString().Trim();
                    var endStr = row.Cell(6).GetString().Trim();
                    if (!string.IsNullOrEmpty(startStr))
                        existing.PlanStartDate = ParseDate(startStr);
                    if (!string.IsNullOrEmpty(endStr))
                        existing.PlanEndDate = ParseDate(endStr);

                    var durVal = row.Cell(7).GetValue<int>();
                    if (durVal > 0)
                        existing.PlanDuration = durVal;

                    taskCodeMap[code] = existing;
                }
                else
                {
                    // 创建新任务
                    var task = new TaskItem
                    {
                        ProjectId = projectId,
                        Code = code,
                        Name = name,
                        SortOrder = row.Cell(1).GetValue<int>(),
                        PlanStartDate = ParseDate(row.Cell(5).GetString()),
                        PlanEndDate = ParseDate(row.Cell(6).GetString()),
                        PlanDuration = row.Cell(7).GetValue<int>(),
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Tasks.Add(task);
                    taskCodeMap[code] = task;
                }

                processedCodes.Add(code);

                // 处理资源分配（如果有资源名称）
                var resourceName = row.Cell(14).GetString().Trim();
                if (!string.IsNullOrEmpty(resourceName))
                {
                    // 保存分配信息，等保存后绑定
                    _context.Entry(taskCodeMap[code]).State = existing != null ? EntityState.Modified : EntityState.Added;
                }
            }
            catch (Exception ex)
            {
                errors.Add($"任务行 {row.RowNumber()}: {ex.Message}");
            }
        }

        await _context.SaveChangesAsync();

        // 第二遍：设置前置关系和资源分配
        foreach (var row in rows)
        {
            try
            {
                var code = row.Cell(2).GetString().Trim();
                if (string.IsNullOrEmpty(code) || !taskCodeMap.ContainsKey(code))
                    continue;

                var task = taskCodeMap[code];

                // 只处理第一次出现的任务（前置关系只在第一行设置）
                var seq = row.Cell(1).GetValue<int>();
                
                // 设置前置任务（J列）
                var predCodes = row.Cell(10).GetString().Trim();
                if (!string.IsNullOrEmpty(predCodes) && !string.IsNullOrEmpty(code))
                {
                    // 检查是否已经是第一行（避免重复添加）
                    var existingRelationCount = task.Predecessors?.Count ?? 0;
                    if (existingRelationCount == 0)
                    {
                        var relationType = ParseRelationType(row.Cell(12).GetString());
                        var lag = row.Cell(11).GetValue<int>();

                        foreach (var predCode in predCodes.Split(',', StringSplitOptions.RemoveEmptyEntries))
                        {
                            var predTrim = predCode.Trim();
                            if (taskCodeMap.ContainsKey(predTrim))
                            {
                                // 检查是否已存在该关系
                                var alreadyExists = await _context.TaskRelations
                                    .AnyAsync(r => r.PredecessorTaskId == taskCodeMap[predTrim].Id && 
                                                   r.SuccessorTaskId == task.Id);
                                
                                if (!alreadyExists)
                                {
                                    var relation = new TaskRelation
                                    {
                                        PredecessorTaskId = taskCodeMap[predTrim].Id,
                                        SuccessorTaskId = task.Id,
                                        Type = relationType,
                                        Lag = lag
                                    };
                                    _context.TaskRelations.Add(relation);
                                }
                            }
                            else
                            {
                                errors.Add($"第{row.RowNumber()}行: 前置任务 {predTrim} 不存在");
                            }
                        }
                    }
                }

                // 处理资源分配（N列: 资源名称, O列: 资源数量, P列: 单位）
                var resourceName = row.Cell(14).GetString().Trim();
                if (!string.IsNullOrEmpty(resourceName))
                {
                    // 查找或创建资源
                    var resource = await _context.Resources
                        .FirstOrDefaultAsync(r => r.ProjectId == projectId && r.Name == resourceName);

                    if (resource == null)
                    {
                        // 自动创建资源
                        resource = new Resource
                        {
                            ProjectId = projectId,
                            Code = $"AUTO-{Guid.NewGuid().ToString().Substring(0, 6)}",
                            Name = resourceName,
                            Type = ResourceType.Labor,
                            Unit = row.Cell(16).GetString().Trim() != "" ? row.Cell(16).GetString().Trim() : "人",
                            Quantity = 1,
                            UnitPrice = 0,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.Resources.Add(resource);
                        await _context.SaveChangesAsync();
                    }

                    // 检查分配是否已存在
                    var existingAssignment = await _context.ResourceAssignments
                        .FirstOrDefaultAsync(a => a.TaskId == task.Id && a.ResourceId == resource.Id);

                    if (existingAssignment == null)
                    {
                        var quantity = row.Cell(15).GetValue<decimal>();
                        var assignment = new ResourceAssignment
                        {
                            TaskId = task.Id,
                            ResourceId = resource.Id,
                            Quantity = quantity > 0 ? quantity : 1,
                            Notes = row.Cell(17).GetString().Trim()
                        };
                        _context.ResourceAssignments.Add(assignment);
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add($"第{row.RowNumber()}行 (前置/资源): {ex.Message}");
            }
        }

        return errors;
    }

    /// <summary>
    /// 导入资源数据（资源数据表）
    /// 格式：资源代码, 资源名称, 资源类型, 单位, 数量, 单价, 小时成本, 备注
    /// </summary>
    private async Task<List<string>> ImportResourcesAsync(int projectId, IXLWorksheet worksheet)
    {
        var errors = new List<string>();
        if (worksheet == null) return errors;

        var existingResources = await _context.Resources
            .Where(r => r.ProjectId == projectId)
            .ToListAsync();

        var rows = worksheet.RowsUsed().Skip(1).ToList();
        foreach (var row in rows)
        {
            try
            {
                var code = row.Cell(1).GetString().Trim();
                var name = row.Cell(2).GetString().Trim();

                if (string.IsNullOrEmpty(code))
                    continue;

                var existing = existingResources.FirstOrDefault(r => r.Code == code);
                if (existing != null)
                {
                    existing.Name = name ?? existing.Name;
                    existing.Type = ParseResourceType(row.Cell(3).GetString());
                    existing.Unit = row.Cell(4).GetString() ?? existing.Unit;
                    var qty = row.Cell(5).GetValue<decimal>();
                    if (qty > 0) existing.Quantity = qty;
                    existing.UnitPrice = row.Cell(6).GetValue<decimal>();
                    existing.HourlyCost = row.Cell(7).GetValue<decimal?>();
                    existing.Notes = row.Cell(8).GetString() ?? existing.Notes;
                }
                else
                {
                    var resource = new Resource
                    {
                        ProjectId = projectId,
                        Code = code,
                        Name = name ?? code,
                        Type = ParseResourceType(row.Cell(3).GetString()),
                        Unit = row.Cell(4).GetString(),
                        Quantity = row.Cell(5).GetValue<decimal>(),
                        UnitPrice = row.Cell(6).GetValue<decimal>(),
                        HourlyCost = row.Cell(7).GetValue<decimal?>(),
                        Notes = row.Cell(8).GetString(),
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Resources.Add(resource);
                }
            }
            catch (Exception ex)
            {
                errors.Add($"资源行 {row.RowNumber()}: {ex.Message}");
            }
        }

        return errors;
    }

    #endregion

    #region 辅助方法

    private DateTime ParseDate(string dateStr)
    {
        if (DateTime.TryParse(dateStr, out var date))
            return date;
        return DateTime.Today;
    }

    private RelationType ParseRelationType(string typeStr)
    {
        return typeStr?.Trim().ToUpper() switch
        {
            "SS" => RelationType.SS,
            "FF" => RelationType.FF,
            "SF" => RelationType.SF,
            _ => RelationType.FS
        };
    }

    private ResourceType ParseResourceType(string typeStr)
    {
        return typeStr?.Trim() switch
        {
            "Material" => ResourceType.Material,
            "Equipment" => ResourceType.Equipment,
            _ => ResourceType.Labor
        };
    }

    #endregion
}
