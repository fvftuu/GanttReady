using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NetPlan.Server.Models;

/// <summary>项目节假日（非工作日）</summary>
public class ProjectHoliday
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ProjectId { get; set; }

    [Required]
    public DateTime Date { get; set; }

    [MaxLength(100)]
    public string? Name { get; set; }              // 节假日名称（如"春节"）

    [ForeignKey("ProjectId")]
    public Project Project { get; set; } = null!;
}
