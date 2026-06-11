using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GanttReady.Server.Models;

public class TaskRelation
{
    [Key]
    public int Id { get; set; }

    public int ProjectId { get; set; }               // 所属项目

    [Required]
    public int PredecessorTaskId { get; set; }      // 紧前任务

    [Required]
    public int SuccessorTaskId { get; set; }        // 紧后任务

    public RelationType Type { get; set; } = RelationType.FS;  // 关系类型

    public int Lag { get; set; }                    // 时差（天），可为负数

    // 导航属性
    [ForeignKey("ProjectId")]
    public Project Project { get; set; } = null!;

    [ForeignKey("PredecessorTaskId")]
    public TaskItem PredecessorTask { get; set; } = null!;

    [ForeignKey("SuccessorTaskId")]
    public TaskItem SuccessorTask { get; set; } = null!;
}
