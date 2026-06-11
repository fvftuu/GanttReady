using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace GanttReady.Server.Services;

/// <summary>
/// Excel导入导出DTO - 任务表
/// </summary>
public class TaskExcelDto
{
    [DisplayName("任务代码")]
    [Description("任务唯一标识，如: T001")]
    public string Code { get; set; } = string.Empty;

    [DisplayName("任务名称")]
    [Description("任务描述名称")]
    public string Name { get; set; } = string.Empty;

    [DisplayName("排序号")]
    [Description("显示顺序，数字越小越靠前")]
    public int SortOrder { get; set; }

    [DisplayName("父任务代码")]
    [Description("上级任务代码，留空表示顶级任务")]
    public string? ParentCode { get; set; }

    [DisplayName("计划开始日期")]
    [Description("格式: yyyy-MM-dd，如: 2024-01-15")]
    public string PlanStartDate { get; set; } = string.Empty;

    [DisplayName("计划结束日期")]
    [Description("格式: yyyy-MM-dd，如: 2024-01-20")]
    public string PlanEndDate { get; set; } = string.Empty;

    [DisplayName("计划工期")]
    [Description("工期天数，如: 5")]
    public int PlanDuration { get; set; }

    [DisplayName("前置任务")]
    [Description("紧前任务代码，多个用逗号分隔，如: T001,T002")]
    public string? Predecessors { get; set; }

    [DisplayName("关系类型")]
    [Description("FS=完成开始, SS=开始开始, FF=完成完成, SF=开始完成")]
    public string RelationType { get; set; } = "FS";

    [DisplayName("时差")]
    [Description("与前置任务的间隔天数，可为负数")]
    public int Lag { get; set; }
}

/// <summary>
/// Excel导入导出DTO - 资源表
/// </summary>
public class ResourceExcelDto
{
    [DisplayName("资源代码")]
    [Description("资源唯一标识，如: R001")]
    public string Code { get; set; } = string.Empty;

    [DisplayName("资源名称")]
    [Description("资源描述名称，如: 施工人员")]
    public string Name { get; set; } = string.Empty;

    [DisplayName("资源类型")]
    [Description("Labor=人工, Material=材料, Equipment=设备")]
    public string Type { get; set; } = "Labor";

    [DisplayName("单位")]
    [Description("计量单位，如: 工日、台班、人天")]
    public string Unit { get; set; } = string.Empty;

    [DisplayName("数量")]
    [Description("可用数量")]
    public decimal Quantity { get; set; }

    [DisplayName("单价")]
    [Description("材料单价（仅材料类型有效），如: 100.00")]
    public decimal UnitPrice { get; set; }

    [DisplayName("小时成本")]
    [Description("人工/设备小时成本，如: 50.00")]
    public decimal? HourlyCost { get; set; }

    [DisplayName("备注")]
    public string? Notes { get; set; }
}

/// <summary>
/// Excel导入导出DTO - 资源分配表
/// </summary>
public class AssignmentExcelDto
{
    [DisplayName("任务代码")]
    [Description("关联的任务代码")]
    public string TaskCode { get; set; } = string.Empty;

    [DisplayName("资源代码")]
    [Description("关联的资源代码")]
    public string ResourceCode { get; set; } = string.Empty;

    [DisplayName("分配数量")]
    [Description("该任务使用该资源的数量")]
    public decimal Quantity { get; set; }

    [DisplayName("备注")]
    public string? Notes { get; set; }
}

/// <summary>
/// Excel导入结果
/// </summary>
public class ExcelImportResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int TasksImported { get; set; }
    public int ResourcesImported { get; set; }
    public int AssignmentsImported { get; set; }
    public List<string> Errors { get; set; } = new();
}
