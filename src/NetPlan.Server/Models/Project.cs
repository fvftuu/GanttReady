using System.ComponentModel.DataAnnotations;

namespace NetPlan.Server.Models;

public class Project
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;           // 项目代号

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;            // 项目名称

    [MaxLength(500)]
    public string? Description { get; set; }                   // 项目描述

    public DateTime PlanStartDate { get; set; }                 // 计划开始日期
    public DateTime PlanEndDate { get; set; }                     // 计划结束日期

    public DateTime? ActualStartDate { get; set; }               // 实际开始日期
    public DateTime? ActualEndDate { get; set; }                 // 实际结束日期

    public int WorkingHoursPerDay { get; set; } = 8;             // 每天工作时间（小时）
    public int WorkdaysPerWeek { get; set; } = 5;                // 每周工作天数

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // 导航属性
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    public ICollection<Resource> Resources { get; set; } = new List<Resource>();
    public ICollection<ColumnDefinition> ColumnDefinitions { get; set; } = new List<ColumnDefinition>();
}
