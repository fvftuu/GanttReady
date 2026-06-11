namespace GanttReady.Server.Models;

public enum RelationType
{
    FS,  // Finish to Start（完成-开始）- 默认
    SS,  // Start to Start（开始-开始）
    SF,  // Start to Finish（开始-完成）
    FF   // Finish to Finish（完成-完成）
}
