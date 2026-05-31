using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NetPlan.Server.Models;

/// <summary>项目基准（快照版本）</summary>
public class Baseline
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ProjectId { get; set; }

    /// <summary>基准编号 1-5</summary>
    public int Number { get; set; }

    [MaxLength(200)]
    public string Name { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("ProjectId")]
    public Project Project { get; set; } = null!;

    public ICollection<BaselineTask> Tasks { get; set; } = new List<BaselineTask>();
}
