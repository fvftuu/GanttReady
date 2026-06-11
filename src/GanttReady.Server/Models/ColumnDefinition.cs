using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GanttReady.Server.Models;

public class ColumnDefinition
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ProjectId { get; set; }

    [Required]
    [MaxLength(50)]
    public string ViewName { get; set; } = string.Empty;        // 视图名称（Gantt/Grid）

    [Required]
    [MaxLength(50)]
    public string FieldName { get; set; } = string.Empty;       // 字段名

    [Required]
    [MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;    // 显示名称

    public int Width { get; set; } = 100;                        // 列宽
    public int SortOrder { get; set; }                           // 排序顺序
    public bool IsVisible { get; set; } = true;                  // 是否可见
    public bool IsEditable { get; set; } = true;                 // 是否可编辑

    // 导航属性
    [ForeignKey("ProjectId")]
    public Project Project { get; set; } = null!;
}
