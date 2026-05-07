using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using NetPlan.Server.Data;
using NetPlan.Server.Models;

namespace NetPlan.Server.Services;

/// <summary>
/// MS Project XML 文件导入服务
/// 支持 Microsoft Project 导出的 XML 格式（namespace: http://schemas.microsoft.com/project）
/// </summary>
public class ProjectXmlImportService
{
    private readonly NetPlanDbContext _db;
    private readonly ILogger<ProjectXmlImportService> _logger;

    private static readonly XNamespace Ns = "http://schemas.microsoft.com/project";

    public ProjectXmlImportService(NetPlanDbContext db, ILogger<ProjectXmlImportService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// 从 MS Project XML 流导入任务和关系
    /// </summary>
    public async Task<ProjectImportResult> ImportAsync(Stream xmlStream, int projectId)
    {
        var result = new ProjectImportResult();
        try
        {
            var doc = XDocument.Load(xmlStream);
            var projectEl = doc.Root;
            if (projectEl == null)
            {
                result.Errors.Add("XML 根元素为空");
                return result;
            }

            // 获取项目日历信息用于日期计算
            var defaultStart = DateTime.Today;
            var calEl = projectEl.Element(Ns + "Calendars");
            var tasksEl = projectEl.Element(Ns + "Tasks");
            if (tasksEl == null)
            {
                result.Errors.Add("未找到 Tasks 元素");
                return result;
            }

            // 第一阶段：解析所有任务
            var taskElements = tasksEl.Elements(Ns + "Task").ToList();
            var uidToId = new Dictionary<int, int>(); // UID → DB Id
            var pendingRelations = new List<(int PredecessorUID, int SuccessorUID, RelationType Type, int Lag)>();

            foreach (var tEl in taskElements)
            {
                var uid = ParseInt(tEl.Element(Ns + "UID")?.Value);
                if (uid <= 0) continue;

                var name = tEl.Element(Ns + "Name")?.Value?.Trim() ?? "未命名任务";
                var startStr = tEl.Element(Ns + "Start")?.Value;
                var finishStr = tEl.Element(Ns + "Finish")?.Value;
                var durationStr = tEl.Element(Ns + "Duration")?.Value;
                var percentStr = tEl.Element(Ns + "PercentComplete")?.Value;
                var milestoneStr = tEl.Element(Ns + "Milestone")?.Value;
                var outlineLevelStr = tEl.Element(Ns + "OutlineLevel")?.Value;
                var outlineLevel = Math.Clamp(ParseInt(outlineLevelStr), 1, 10);
                var notesStr = tEl.Element(Ns + "Notes")?.Value;
                var wbsStr = tEl.Element(Ns + "WBS")?.Value;

                var planStart = DateTime.TryParse(startStr, out var s) ? s : defaultStart;
                var planEnd = DateTime.TryParse(finishStr, out var f) ? f : planStart.AddDays(1);
                var duration = ParseDuration(durationStr); // PT 格式 → 天数

                // 日期校验：拒绝超过10年或早于2000年的异常日期
                var maxDate = DateTime.Today.AddYears(10);
                if (planStart > maxDate || planStart.Year < 2000)
                    planStart = defaultStart;
                if (planEnd > maxDate || planEnd.Year < 2000 || planEnd < planStart)
                    planEnd = planStart.AddDays(Math.Max(1, duration)) - TimeSpan.FromDays(1);

                var percent = Math.Clamp(ParseInt(percentStr), 0, 100);

                var task = new TaskItem
                {
                    ProjectId = projectId,
                    Name = name,
                    Code = wbsStr ?? "",
                    PlanStartDate = planStart,
                    PlanEndDate = planEnd,
                    PlanDuration = Math.Max(1, duration),
                    CompletionPercentage = percent,
                    IsMilestone = milestoneStr == "1",
                    OutlineLevel = outlineLevel,
                    SortOrder = result.TasksImported,
                    IsManualSchedule = false  // 导入后由 CPM 重算日期
                };

                _db.Tasks.Add(task);
                await _db.SaveChangesAsync(); // 获取 Id
                uidToId[uid] = task.Id;
                result.TasksImported++;
            }

            // 第1.5阶段：从 OutlineLevel 重建父子层级（仅处理新导入的任务）
            var newIds = uidToId.Values.ToHashSet();
            var newTasks = _db.Tasks.Where(t => t.ProjectId == projectId && newIds.Contains(t.Id))
                .OrderBy(t => t.SortOrder).ToList();
            var uidOrder = taskElements.Select(e => ParseInt(e.Element(Ns + "UID")?.Value)).ToList();
            foreach (var task in newTasks.Where(t => t.OutlineLevel > 1))
            {
                // 在同批导入中找最近的低层级任务作为父任务
                var idx = newTasks.IndexOf(task);
                for (int i = idx - 1; i >= 0; i--)
                {
                    if (newTasks[i].OutlineLevel < task.OutlineLevel)
                    {
                        task.ParentTaskId = newTasks[i].Id;
                        await _db.SaveChangesAsync();
                        break;
                    }
                }
            }

            // 重算父任务工期 = 子任务工期汇总
            foreach (var parent in newTasks.Where(t =>
                newTasks.Any(c => c.ParentTaskId == t.Id)))
            {
                var children = newTasks.Where(t => t.ParentTaskId == parent.Id).ToList();
                if (children.Any())
                {
                    parent.PlanStartDate = children.Min(c => c.PlanStartDate);
                    parent.PlanEndDate = children.Max(c => c.PlanEndDate);
                    parent.PlanDuration = Math.Max(1, (parent.PlanEndDate - parent.PlanStartDate).Days + 1);
                    parent.IsManualSchedule = false;
                }
            }

            // 第二阶段：解析前置关系
            foreach (var tEl in taskElements)
            {
                var uid = ParseInt(tEl.Element(Ns + "UID")?.Value);
                if (uid <= 0 || !uidToId.ContainsKey(uid)) continue;

                foreach (var predEl in tEl.Elements(Ns + "PredecessorLink"))
                {
                    var predUid = ParseInt(predEl.Element(Ns + "PredecessorUID")?.Value);
                    var typeVal = ParseInt(predEl.Element(Ns + "Type")?.Value);
                    var lagVal = ParseDuration(predEl.Element(Ns + "LinkLag")?.Value);

                    if (predUid <= 0 || !uidToId.ContainsKey(predUid) || !uidToId.ContainsKey(uid))
                        continue;

                    var relType = typeVal switch
                    {
                        0 => RelationType.FF,
                        1 => RelationType.FS,
                        2 => RelationType.SF,
                        3 => RelationType.SS,
                        _ => RelationType.FS
                    };

                    var relation = new TaskRelation
                    {
                        PredecessorTaskId = uidToId[predUid],
                        SuccessorTaskId = uidToId[uid],
                        Type = relType,
                        Lag = lagVal
                    };
                    _db.TaskRelations.Add(relation);
                    result.RelationsImported++;
                }
            }

            await _db.SaveChangesAsync();
            result.Success = true;
            result.Message = $"成功导入 {result.TasksImported} 个任务，{result.RelationsImported} 个关系";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Project XML 导入失败");
            result.Errors.Add(ex.Message);
        }

        return result;
    }

    /// <summary>
    /// 解析 MS Project 时长格式 (PT8H0M0S → 天数)
    /// 默认: PT8H = 1 天
    /// </summary>
    private static int ParseDuration(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return 1;

        // PT8H0M0S → 提取小时+分钟，按每天 workHoursPerDay 小时换算天数
        const int workHoursPerDay = 8;
        var hrs = 0;
        var mins = 0;

        // 匹配天数格式: PT1D
        var dayMatch = System.Text.RegularExpressions.Regex.Match(value, @"(\d+)D");
        if (dayMatch.Success)
        {
            return Math.Max(1, int.Parse(dayMatch.Groups[1].Value));
        }

        // 匹配小时: PT8H
        var hMatch = System.Text.RegularExpressions.Regex.Match(value, @"(\d+)H");
        if (hMatch.Success) hrs = int.Parse(hMatch.Groups[1].Value);

        // 匹配分钟: PT30M
        var mMatch = System.Text.RegularExpressions.Regex.Match(value, @"(\d+)M");
        if (mMatch.Success) mins = int.Parse(mMatch.Groups[1].Value);

        var totalHours = hrs + mins / 60.0;
        return Math.Max(1, (int)Math.Ceiling(totalHours / workHoursPerDay));
    }

    private static int ParseInt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return 0;
        return int.TryParse(value, out var n) ? n : 0;
    }
}

public class ProjectImportResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public int TasksImported { get; set; }
    public int RelationsImported { get; set; }
    public List<string> Errors { get; set; } = new();
}
