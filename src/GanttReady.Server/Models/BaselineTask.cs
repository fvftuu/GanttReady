using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GanttReady.Server.Models;

/// <summary>基准中的任务快照</summary>
public class BaselineTask
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int BaselineId { get; set; }

    [Required]
    public int TaskId { get; set; }

    public DateTime PlanStartDate { get; set; }
    public DateTime PlanEndDate { get; set; }
    public int PlanDuration { get; set; }

    [ForeignKey("BaselineId")]
    public Baseline Baseline { get; set; } = null!;
}
