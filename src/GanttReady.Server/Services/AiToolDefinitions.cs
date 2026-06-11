namespace GanttReady.Server.Services;

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
            FindProjectTool(),
            CreateProjectWithTasksTool(),
            CreateProjectTool(),
            GetAllProjectsTool(),
            CreateProjectFromJsonTool(),
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
    /// 全部可用工具（projectId > 0 时项目工具也会包含）
    /// </summary>
    public static List<AiToolDefinition> GetAllTools(int? projectId = null)
    {
        var tools = GetGlobalTools();
        if (projectId > 0)
            tools.AddRange(GetProjectTools(projectId.Value));
        return tools;
    }

    /// <summary>
    /// 链式调用用工具列表——始终包含所有工具，不限制项目
    /// </summary>
    public static List<AiToolDefinition> GetAllChainTools()
    {
        // 为每个项目工具手动创建定义，避免不能与具体 projectId 绑定
        var tools = GetGlobalTools().Concat(GetProjectTools(0)).ToList();
        // 分析工具在所有场景下可用
        tools.AddRange(GetAnalysisTools());
        return tools;
    }

    /// <summary>
    /// 分析工具（不依赖 projectId 绑定，由工具参数指定）
    /// </summary>
    public static List<AiToolDefinition> GetAnalysisTools()
    {
        return new List<AiToolDefinition>
        {
            ProjectOverviewTool(),
            EvmAnalysisTool(),
            StageCompletionTool(),
            ScheduleVarianceTool(),
            CriticalPathTool(),
        };
    }

    // ==================== 工具定义 ====================

    public static AiToolDefinition FindProjectTool()
    {
        return new AiToolDefinition
        {
            Name = "find_project",
            Description = "根据项目名称模糊搜索匹配的项目，返回项目ID、名称、起止日期。当用户提到项目名但未指定 projectId 时，先调用此工具找到项目ID，再用 ID 调用其他工具。",
            Parameters = new
            {
                type = "object",
                properties = new
                {
                    name = new { type = "string", description = "要搜索的项目名称，支持模糊匹配" }
                },
                required = new[] { "name" }
            }
        };
    }

    public static AiToolDefinition CreateProjectWithTasksTool()
    {
        return new AiToolDefinition
        {
            Name = "create_project_with_tasks",
            Description = "一次性创建项目及其所有任务。用户用自然语言描述项目计划（工程/营销/活动/软件等各行业）时使用此工具。返回项目ID和任务数量。",
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

    public static AiToolDefinition CreateProjectFromJsonTool()
    {
        return new AiToolDefinition
        {
            Name = "create_project_from_json",
            Description = "接收完整的项目 JSON，一次创建项目+所有任务+资源。适用于用户描述完整的项目计划时使用。返回预览让用户确认后再创建。",
            Parameters = new
            {
                type = "object",
                properties = new
                {
                    project_json = new
                    {
                        type = "string",
                        description = @"完整的项目 JSON 字符串，结构如下：
{
  projectName: 项目名称,
  projectDescription: 项目描述,
  planStartDate: 计划开始日期 yyyy-MM-dd,
  totalDuration: 总工期参考值（系统按前置关系自动排程）,
  tasks: [
    {
      code: 任务代号,
      name: 任务名称,
      duration: 工期（自然日）,
      workType: ""marine"" 或 ""land""（水上按15天/月，陆上按24天/月）,
      predecessors: [紧前任务code列表],
      assignee: 负责人,
      resources: [设备名称列表]
    }
  ],
  resources: [
    { name: 资源名, type: equipment/labor/material, quantity: 数量 }
  ]
}"
                    }
                },
                required = new[] { "project_json" }
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

    // ==================== 分析工具定义 ====================

    public static AiToolDefinition ProjectOverviewTool()
    {
        return new AiToolDefinition
        {
            Name = "get_project_overview",
            Description = "获取项目概况：总任务数、完成率、关键路径任务数、延迟任务数、资源分配数。在用户询问项目整体状况时优先使用。",
            Parameters = new
            {
                type = "object",
                properties = new
                {
                    projectId = new { type = "integer", description = "项目ID" }
                },
                required = new[] { "projectId" }
            }
        };
    }

    public static AiToolDefinition EvmAnalysisTool()
    {
        return new AiToolDefinition
        {
            Name = "get_evm_analysis",
            Description = "获取挣值分析（EVM）结果：计划价值PV、挣值EV、实际成本AC、进度绩效SPI、成本绩效CPI、进度偏差SV、成本偏差CV、综合绩效CSI、总预算BAC、估算完工EAC、月度SPI趋势。在用户询问进度绩效、成本绩效时使用。",
            Parameters = new
            {
                type = "object",
                properties = new
                {
                    projectId = new { type = "integer", description = "项目ID" }
                },
                required = new[] { "projectId" }
            }
        };
    }

    public static AiToolDefinition StageCompletionTool()
    {
        return new AiToolDefinition
        {
            Name = "get_stage_completion",
            Description = "【阶段分组数据】获取按里程碑/WBS分组的阶段完成率。⚠️ 每个阶段包含多个任务，阶段统计（任务数/完成率）不能引用到单个任务上。返回各阶段名称、计划完成%、实际完成%、阶段内任务总数/已完成数、延迟天数、状态。在用户询问各阶段或分组完成情况时使用。",
            Parameters = new
            {
                type = "object",
                properties = new
                {
                    projectId = new { type = "integer", description = "项目ID" }
                },
                required = new[] { "projectId" }
            }
        };
    }

    public static AiToolDefinition ScheduleVarianceTool()
    {
        return new AiToolDefinition
        {
            Name = "get_schedule_variance",
            Description = "获取工期偏差分析：提前/按时/延后任务数量及列表、总偏差天数。在用户询问哪些任务滞后、进度偏差时使用。",
            Parameters = new
            {
                type = "object",
                properties = new
                {
                    projectId = new { type = "integer", description = "项目ID" }
                },
                required = new[] { "projectId" }
            }
        };
    }

    public static AiToolDefinition CriticalPathTool()
    {
        return new AiToolDefinition
        {
            Name = "get_critical_path",
            Description = "获取关键路径：所有关键路径任务的代码、名称、计划起止日期、工期、时差、负责人。在用户询问关键路径、瓶颈任务时使用。",
            Parameters = new
            {
                type = "object",
                properties = new
                {
                    projectId = new { type = "integer", description = "项目ID" }
                },
                required = new[] { "projectId" }
            }
        };
    }
}
