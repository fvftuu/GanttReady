using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NetPlan.Server.Models;

public class TaskItem
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ProjectId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;            // 任务代号

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;            // 任务名称

    public int SortOrder { get; set; }                          // 排序顺序

    // 支持 WBS 层级（父子任务）
    public int? ParentTaskId { get; set; }

    // 计划信息
    public DateTime PlanStartDate { get; set; }
    public DateTime PlanEndDate { get; set; }
    public int PlanDuration { get; set; }                        // 计划工期（天）

    // 实际信息
    public DateTime? ActualStartDate { get; set; }
    public DateTime? ActualEndDate { get; set; }
    public int? ActualDuration { get; set; }                    // 实际工期

    // 计算字段（调度引擎填充）
    public int? EarlyStart { get; set; }                          // 最早开始（相对于项目开始的天数）
    public int? EarlyFinish { get; set; }                         // 最早完成
    public int? LateStart { get; set; }                           // 最迟开始
    public int? LateFinish { get; set; }                          // 最迟完成
    public int? TotalFloat { get; set; }                          // 总时差
    public int? FreeFloat { get; set; }                           // 自由时差
    public bool IsCritical { get; set; }                          // 是否关键工序

    // 扩展字段（JSON 存储）
    public string? ExtraData { get; set; }                       // 扩展数据

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;  // 创建时间

    // 导航属性
    [ForeignKey("ProjectId")]
    public Project Project { get; set; } = null!;

    [ForeignKey("ParentTaskId")]
    public TaskItem? ParentTask { get; set; }

    public ICollection<TaskItem> SubTasks { get; set; } = new List<TaskItem>();

    [InverseProperty("SuccessorTask")]
    public ICollection<TaskRelation> Predecessors { get; set; } = new List<TaskRelation>();

    [InverseProperty("PredecessorTask")]
    public ICollection<TaskRelation> Successors { get; set; } = new List<TaskRelation>();

    public ICollection<ResourceAssignment> ResourceAssignments { get; set; } = new List<ResourceAssignment>();
}
