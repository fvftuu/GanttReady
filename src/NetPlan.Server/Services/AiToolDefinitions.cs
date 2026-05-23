namespace NetPlan.Server.Services;

/// <summary>
/// AI 工具定义（function calling schema）
/// </summary>
public static class AiToolDefinitions
{
    /// <summary>
    /// 全局工具（不依赖具体项目）
    /// </summary>
    public static List<AiToolDefinition> GetGlobalTools()
    {
        return new List<AiToolDefinition>
        {
            CreateProjectWithTasksTool(),
            CreateProjectTool(),
            GetAllProjectsTool(),
        };
    }

    /// <summary>
    /// 项目相关工具（需要 projectId）
    /// </summary>
    public static List<AiToolDefinition> GetProjectTools(int projectId)
    {
        return new List<AiToolDefinition>
        {
            CreateTaskTool(projectId),
            UpdateTaskTool(projectId),
            GetTasksTool(projectId),
        };
    }

    /// <summary>
    /// 全部可用工具
    /// </summary>
    public static List<AiToolDefinition> GetAllTools(int? projectId = null)
    {
        var tools = GetGlobalTools();
        if (projectId > 0)
            tools.AddRange(GetProjectTools(projectId.Value));
        return tools;
    }

    // ==================== 工具定义 ====================

    public static AiToolDefinition CreateProjectWithTasksTool()
    {
        return new AiToolDefinition
        {
            Name = "create_project_with_tasks",
            Description = "【推荐】创建一个新项目及其所有任务（一次性完成）。用户通过概念性描述项目计划时使用此工具。项目创建后返回项目ID和所有任务数量。",
            Parameters = new
            {
                type = "object",
                properties = new
                {
                    name = new { type = "string", description = "项目名称" },
                    code = new { type = "string", description = "项目代号" },
                    description = new { type = "string", description = "项目整体描述" },
                    planStartDate = new { type = "string", description = "计划开始日期，格式 yyyy-MM-dd" },
                    planEndDate = new { type = "string", description = "计划结束日期，格式 yyyy-MM-dd" },
                    tasks = new
                    {
                        type = "array",
                        description = "项目包含的所有任务列表，按执行顺序排列",
                        items = new
                        {
                            type = "object",
                            properties = new
                            {
                                name = new { type = "string", description = "任务名称" },
                                code = new { type = "string", description = "任务代号（短名称）" },
                                planDuration = new { type = "integer", description = "计划工期（天）" },
                                planStartDate = new { type = "string", description = "计划开始日期，格式 yyyy-MM-dd，不填由系统自动推算" },
                                planEndDate = new { type = "string", description = "计划结束日期，格式 yyyy-MM-dd，不填由系统自动推算" },
                                responsiblePerson = new { type = "string", description = "负责人" },
                                isMilestone = new { type = "boolean", description = "是否为里程碑任务" },
                                parentTaskName = new { type = "string", description = "父任务名称（用于WBS层级分组）" },
                                predecessorNames = new
                                {
                                    type = "array",
                                    items = new { type = "string" },
                                    description = "前置任务名称列表（用名称匹配）"
                                }
                            },
                            required = new[] { "name", "planDuration" }
                        }
                    }
                },
                required = new[] { "name", "tasks" }
            }
        };
    }

    public static AiToolDefinition CreateProjectTool()
    {
        return new AiToolDefinition
        {
            Name = "create_project",
            Description = "创建一个新项目。创建后返回项目ID和详细信息。",
            Parameters = new
            {
                type = "object",
                properties = new
                {
                    name = new { type = "string", description = "项目名称" },
                    code = new { type = "string", description = "项目代号（短名称，如 PM-2026-001）" },
                    description = new { type = "string", description = "项目描述" },
                    planStartDate = new { type = "string", description = "计划开始日期，格式 yyyy-MM-dd" },
                    planEndDate = new { type = "string", description = "计划结束日期，格式 yyyy-MM-dd" }
                },
                required = new[] { "name" }
            }
        };
    }

    public static AiToolDefinition GetAllProjectsTool()
    {
        return new AiToolDefinition
        {
            Name = "get_all_projects",
            Description = "获取所有项目的列表。",
            Parameters = new
            {
                type = "object",
                properties = new { },
                required = Array.Empty<string>()
            }
        };
    }

    public static AiToolDefinition CreateTaskTool(int projectId)
    {
        return new AiToolDefinition
        {
            Name = "create_task",
            Description = "在项目中创建一个新任务。创建后返回任务ID和详细信息。可以指定前置任务名称来建立依赖关系。",
            Parameters = new
            {
                type = "object",
                properties = new
                {
                    projectId = new { type = "integer", description = "所属项目ID" },
                    name = new { type = "string", description = "任务名称" },
                    planDuration = new { type = "integer", description = "计划工期（天）" },
                    planStartDate = new { type = "string", description = "计划开始日期，格式 yyyy-MM-dd" },
                    planEndDate = new { type = "string", description = "计划结束日期，格式 yyyy-MM-dd" },
                    responsiblePerson = new { type = "string", description = "负责人" },
                    isMilestone = new { type = "boolean", description = "是否为里程碑" },
                    parentTaskName = new { type = "string", description = "父任务名称（用于建立WBS层级）" },
                    predecessorNames = new
                    {
                        type = "array",
                        items = new { type = "string" },
                        description = "前置任务名称列表（用于建立依赖关系）"
                    }
                },
                required = new[] { "projectId", "name", "planDuration" }
            }
        };
    }

    public static AiToolDefinition UpdateTaskTool(int projectId)
    {
        return new AiToolDefinition
        {
            Name = "update_task",
            Description = "更新已有任务的属性。只传需要修改的字段，不传的字段保持不变。",
            Parameters = new
            {
                type = "object",
                properties = new
                {
                    projectId = new { type = "integer", description = "所属项目ID" },
                    taskName = new { type = "string", description = "要修改的任务名称（用于查找任务）" },
                    newName = new { type = "string", description = "新的任务名称" },
                    planDuration = new { type = "integer", description = "新的计划工期（天）" },
                    planStartDate = new { type = "string", description = "新的计划开始日期，格式 yyyy-MM-dd" },
                    planEndDate = new { type = "string", description = "新的计划结束日期，格式 yyyy-MM-dd" },
                    responsiblePerson = new { type = "string", description = "新的负责人" },
                    completionPercent = new { type = "integer", description = "完成百分比 0-100" }
                },
                required = new[] { "projectId", "taskName" }
            }
        };
    }

    public static AiToolDefinition GetTasksTool(int projectId)
    {
        return new AiToolDefinition
        {
            Name = "get_tasks",
            Description = "获取项目当前所有任务的列表。用于在创建/修改任务前了解现有任务。",
            Parameters = new
            {
                type = "object",
                properties = new
                {
                    projectId = new { type = "integer", description = "所属项目ID" }
                },
                required = new[] { "projectId" }
            }
        };
    }
}
