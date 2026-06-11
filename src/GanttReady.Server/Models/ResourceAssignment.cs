using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GanttReady.Server.Models;

/// <summary>
/// 资源分配（任务-资源关联）
/// </summary>
public class ResourceAssignment
{
    [Key]
    public int Id { get; set; }

    public int TaskId { get; set; }
    public int ResourceId { get; set; }

    public decimal Quantity { get; set; }           // 分配数量

    [MaxLength(500)]
    public string? Notes { get; set; }             // 备注

    // 导航属性
    [ForeignKey("TaskId")]
    public TaskItem Task { get; set; } = null!;

    [ForeignKey("ResourceId")]
    public Resource Resource { get; set; } = null!;
}
