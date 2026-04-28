using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NetPlan.Server.Models;

public class Resource
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ProjectId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;            // 资源代号

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;            // 资源名称

    public ResourceType Type { get; set; }                       // 资源类型

    [MaxLength(20)]
    public string Unit { get; set; } = string.Empty;            // 单位

    public decimal Quantity { get; set; }                        // 数量
    public decimal UnitPrice { get; set; }                       // 单价（对Cost类型有效）
    public decimal? HourlyCost { get; set; }                     // 成本/小时（人员/设备用）

    [MaxLength(500)]
    public string? Notes { get; set; }                            // 备注

    // 扩展字段（JSON 存储）
    public string? ExtraData { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;  // 创建时间

    // 导航属性
    [ForeignKey("ProjectId")]
    public Project Project { get; set; } = null!;

    public ICollection<ResourceAssignment> Assignments { get; set; } = new List<ResourceAssignment>();
}
