using NetPlan.Server.Models;

namespace NetPlan.Server.Data;

/// <summary>
/// 示例数据初始化
/// </summary>
public static class DataSeeder
{
    public static void SeedDemoData(NetPlanDbContext db)
    {
        // 只管理示例数据，不要删除用户创建的项目
        const string DEMO_PROJECT_CODE = "HP-2026-001";
        
        if (db.Projects.Any(p => p.Code == DEMO_PROJECT_CODE))
            return; // 示例项目已存在，跳过

        var project = new Project
        {
            Code = "HP-2026-001",
            Name = "综合基础设施建设项目",
            Description = "大型综合基础设施工程，涵盖土建、机电、装修等全周期施工",
            PlanStartDate = new DateTime(2026, 1, 1),
            PlanEndDate   = new DateTime(2028, 12, 31),
            WorkingHoursPerDay = 8,
            WorkdaysPerWeek  = 5,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Projects.Add(project);
        db.SaveChanges();

        var startDate = new DateTime(2026, 1, 1);
        var tasks = new List<TaskItem>
        {
            // ==================== A阶段：施工准备 (1-5) ====================
            new() { ProjectId = project.Id, Code = "A1",  Name = "项目立项与审批",     SortOrder = 1,  PlanStartDate = startDate,                       PlanEndDate = startDate.AddDays(29),   PlanDuration = 30,  CompletionPercentage = 100, CreatedAt = DateTime.UtcNow },
            new() { ProjectId = project.Id, Code = "A2",  Name = "施工图设计",         SortOrder = 2,  PlanStartDate = startDate.AddDays(15),           PlanEndDate = startDate.AddDays(134),  PlanDuration = 120, CompletionPercentage = 65,  CreatedAt = DateTime.UtcNow },
            new() { ProjectId = project.Id, Code = "A3",  Name = "施工现场临建",        SortOrder = 3,  PlanStartDate = startDate.AddDays(30),           PlanEndDate = startDate.AddDays(59),   PlanDuration = 30,  CompletionPercentage = 100, CreatedAt = DateTime.UtcNow },
            new() { ProjectId = project.Id, Code = "A4",  Name = "施工组织设计编制",    SortOrder = 4,  PlanStartDate = startDate.AddDays(30),           PlanEndDate = startDate.AddDays(59),   PlanDuration = 30,  CompletionPercentage = 100, CreatedAt = DateTime.UtcNow },
            new() { ProjectId = project.Id, Code = "A5",  Name = "施工设备进场",        SortOrder = 5,  PlanStartDate = startDate.AddDays(45),           PlanEndDate = startDate.AddDays(74),   PlanDuration = 30,  CompletionPercentage = 100, CreatedAt = DateTime.UtcNow },

            // ==================== B阶段：基础工程 (6-10) ====================
            new() { ProjectId = project.Id, Code = "B1",  Name = "场地平整与大开挖",   SortOrder = 6,  PlanStartDate = startDate.AddDays(60),           PlanEndDate = startDate.AddDays(149),  PlanDuration = 90,  CompletionPercentage = 30,  CreatedAt = DateTime.UtcNow },
            new() { ProjectId = project.Id, Code = "B2",  Name = "基坑支护工程",       SortOrder = 7,  PlanStartDate = startDate.AddDays(90),           PlanEndDate = startDate.AddDays(179),  PlanDuration = 90,  CreatedAt = DateTime.UtcNow },
            new() { ProjectId = project.Id, Code = "B3",  Name = "桩基施工",           SortOrder = 8,  PlanStartDate = startDate.AddDays(120),          PlanEndDate = startDate.AddDays(269),  PlanDuration = 150, CreatedAt = DateTime.UtcNow },
            new() { ProjectId = project.Id, Code = "B4",  Name = "地下室结构",          SortOrder = 9,  PlanStartDate = startDate.AddDays(180),          PlanEndDate = startDate.AddDays(359),  PlanDuration = 180, CreatedAt = DateTime.UtcNow },
            new() { ProjectId = project.Id, Code = "B5",  Name = "防水工程",           SortOrder = 10, PlanStartDate = startDate.AddDays(300),          PlanEndDate = startDate.AddDays(389),  PlanDuration = 90,  CreatedAt = DateTime.UtcNow },

            // ==================== C阶段：主体结构 (11-18) ====================
            new() { ProjectId = project.Id, Code = "C1",  Name = "地上结构一层",       SortOrder = 11, PlanStartDate = startDate.AddDays(330),          PlanEndDate = startDate.AddDays(419),  PlanDuration = 90,  CreatedAt = DateTime.UtcNow },
            new() { ProjectId = project.Id, Code = "C2",  Name = "地上结构二层",       SortOrder = 12, PlanStartDate = startDate.AddDays(360),          PlanEndDate = startDate.AddDays(449),  PlanDuration = 90,  CreatedAt = DateTime.UtcNow },
            new() { ProjectId = project.Id, Code = "C3",  Name = "地上结构三层",       SortOrder = 13, PlanStartDate = startDate.AddDays(390),          PlanEndDate = startDate.AddDays(479),  PlanDuration = 90,  CreatedAt = DateTime.UtcNow },
            new() { ProjectId = project.Id, Code = "C4",  Name = "地上结构四层",       SortOrder = 14, PlanStartDate = startDate.AddDays(420),          PlanEndDate = startDate.AddDays(509),  PlanDuration = 90,  CreatedAt = DateTime.UtcNow },
            new() { ProjectId = project.Id, Code = "C5",  Name = "地上结构五层",       SortOrder = 15, PlanStartDate = startDate.AddDays(450),          PlanEndDate = startDate.AddDays(539),  PlanDuration = 90,  CreatedAt = DateTime.UtcNow },
            new() { ProjectId = project.Id, Code = "C6",  Name = "主体结构封顶",       SortOrder = 16, PlanStartDate = startDate.AddDays(480),          PlanEndDate = startDate.AddDays(549),  PlanDuration = 70,  IsMilestone = true, CreatedAt = DateTime.UtcNow },
            new() { ProjectId = project.Id, Code = "C7",  Name = "屋面工程",           SortOrder = 17, PlanStartDate = startDate.AddDays(540),          PlanEndDate = startDate.AddDays(629),  PlanDuration = 90,  CreatedAt = DateTime.UtcNow },
            new() { ProjectId = project.Id, Code = "C8",  Name = "外墙围护结构",       SortOrder = 18, PlanStartDate = startDate.AddDays(550),          PlanEndDate = startDate.AddDays(729),  PlanDuration = 180, CreatedAt = DateTime.UtcNow },

            // ==================== D阶段：机电安装 (19-24) ====================
            new() { ProjectId = project.Id, Code = "D1",  Name = "电气系统安装",       SortOrder = 19, PlanStartDate = startDate.AddDays(450),          PlanEndDate = startDate.AddDays(809),  PlanDuration = 360, CreatedAt = DateTime.UtcNow },
            new() { ProjectId = project.Id, Code = "D2",  Name = "给排水系统",         SortOrder = 20, PlanStartDate = startDate.AddDays(450),          PlanEndDate = startDate.AddDays(779),  PlanDuration = 330, CreatedAt = DateTime.UtcNow },
            new() { ProjectId = project.Id, Code = "D3",  Name = "通风与空调",         SortOrder = 21, PlanStartDate = startDate.AddDays(500),          PlanEndDate = startDate.AddDays(839),  PlanDuration = 340, CreatedAt = DateTime.UtcNow },
            new() { ProjectId = project.Id, Code = "D4",  Name = "消防系统",           SortOrder = 22, PlanStartDate = startDate.AddDays(550),          PlanEndDate = startDate.AddDays(869),  PlanDuration = 320, CreatedAt = DateTime.UtcNow },
            new() { ProjectId = project.Id, Code = "D5",  Name = "弱电智能化系统",     SortOrder = 23, PlanStartDate = startDate.AddDays(600),          PlanEndDate = startDate.AddDays(959),  PlanDuration = 360, CreatedAt = DateTime.UtcNow },
            new() { ProjectId = project.Id, Code = "D6",  Name = "电梯安装",           SortOrder = 24, PlanStartDate = startDate.AddDays(700),          PlanEndDate = startDate.AddDays(989),  PlanDuration = 290, CreatedAt = DateTime.UtcNow },

            // ==================== E阶段：装修与收尾 (25-30) ====================
            new() { ProjectId = project.Id, Code = "E1",  Name = "室内装修工程",       SortOrder = 25, PlanStartDate = startDate.AddDays(730),          PlanEndDate = startDate.AddDays(989),  PlanDuration = 260, CreatedAt = DateTime.UtcNow },
            new() { ProjectId = project.Id, Code = "E2",  Name = "景观绿化工程",       SortOrder = 26, PlanStartDate = startDate.AddDays(800),          PlanEndDate = startDate.AddDays(1019), PlanDuration = 220, CreatedAt = DateTime.UtcNow },
            new() { ProjectId = project.Id, Code = "E3",  Name = "室外管网工程",       SortOrder = 27, PlanStartDate = startDate.AddDays(750),          PlanEndDate = startDate.AddDays(929),  PlanDuration = 180, CreatedAt = DateTime.UtcNow },
            new() { ProjectId = project.Id, Code = "E4",  Name = "道路与广场",         SortOrder = 28, PlanStartDate = startDate.AddDays(850),          PlanEndDate = startDate.AddDays(1019), PlanDuration = 170, CreatedAt = DateTime.UtcNow },
            new() { ProjectId = project.Id, Code = "E5",  Name = "竣工验收准备",       SortOrder = 29, PlanStartDate = startDate.AddDays(980),          PlanEndDate = startDate.AddDays(1049), PlanDuration = 70,  CreatedAt = DateTime.UtcNow },
            new() { ProjectId = project.Id, Code = "E6",  Name = "竣工验收交付",       SortOrder = 30, PlanStartDate = startDate.AddDays(1050),         PlanEndDate = startDate.AddDays(1094), PlanDuration = 45,  IsMilestone = true, CreatedAt = DateTime.UtcNow },
        };
        db.Tasks.AddRange(tasks);
        db.SaveChanges();

        // 创建任务关系
        var relations = new List<TaskRelation>
        {
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[0].Id, SuccessorTaskId = tasks[1].Id, Type = RelationType.FS, Lag = 0 },
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[0].Id, SuccessorTaskId = tasks[2].Id, Type = RelationType.FS, Lag = 0 },
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[1].Id, SuccessorTaskId = tasks[3].Id, Type = RelationType.FS, Lag = 0 },
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[2].Id, SuccessorTaskId = tasks[4].Id, Type = RelationType.FS, Lag = 0 },
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[3].Id, SuccessorTaskId = tasks[4].Id, Type = RelationType.FS, Lag = 0 },
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[4].Id, SuccessorTaskId = tasks[5].Id, Type = RelationType.FS, Lag = 0 },
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[5].Id, SuccessorTaskId = tasks[6].Id, Type = RelationType.FS, Lag = 15 },
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[6].Id, SuccessorTaskId = tasks[7].Id, Type = RelationType.FS, Lag = 0 },
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[7].Id, SuccessorTaskId = tasks[8].Id, Type = RelationType.FS, Lag = 30 },
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[8].Id, SuccessorTaskId = tasks[9].Id, Type = RelationType.FS, Lag = 30 },
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[9].Id, SuccessorTaskId = tasks[10].Id, Type = RelationType.FS, Lag = 0 },
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[10].Id, SuccessorTaskId = tasks[11].Id, Type = RelationType.FS, Lag = 0 },
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[11].Id, SuccessorTaskId = tasks[12].Id, Type = RelationType.FS, Lag = 0 },
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[12].Id, SuccessorTaskId = tasks[13].Id, Type = RelationType.FS, Lag = 0 },
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[13].Id, SuccessorTaskId = tasks[14].Id, Type = RelationType.FS, Lag = 0 },
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[14].Id, SuccessorTaskId = tasks[15].Id, Type = RelationType.FS, Lag = 0 },
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[15].Id, SuccessorTaskId = tasks[16].Id, Type = RelationType.FS, Lag = 30 },
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[15].Id, SuccessorTaskId = tasks[17].Id, Type = RelationType.FS, Lag = 0 },
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[15].Id, SuccessorTaskId = tasks[18].Id, Type = RelationType.FS, Lag = 0 },
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[15].Id, SuccessorTaskId = tasks[19].Id, Type = RelationType.FS, Lag = 0 },
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[15].Id, SuccessorTaskId = tasks[20].Id, Type = RelationType.FS, Lag = 20 },
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[15].Id, SuccessorTaskId = tasks[21].Id, Type = RelationType.FS, Lag = 30 },
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[15].Id, SuccessorTaskId = tasks[22].Id, Type = RelationType.FS, Lag = 60 },
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[15].Id, SuccessorTaskId = tasks[23].Id, Type = RelationType.FS, Lag = 180 },
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[22].Id, SuccessorTaskId = tasks[24].Id, Type = RelationType.FS, Lag = 30 },
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[23].Id, SuccessorTaskId = tasks[24].Id, Type = RelationType.FS, Lag = 0 },
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[17].Id, SuccessorTaskId = tasks[25].Id, Type = RelationType.FS, Lag = 60 },
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[17].Id, SuccessorTaskId = tasks[26].Id, Type = RelationType.FS, Lag = 60 },
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[26].Id, SuccessorTaskId = tasks[27].Id, Type = RelationType.FS, Lag = 30 },
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[24].Id, SuccessorTaskId = tasks[28].Id, Type = RelationType.FS, Lag = 0 },
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[27].Id, SuccessorTaskId = tasks[28].Id, Type = RelationType.FS, Lag = 0 },
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[28].Id, SuccessorTaskId = tasks[29].Id, Type = RelationType.FS, Lag = 0 },
        };
        db.TaskRelations.AddRange(relations);

        // Seed resources
        var resources = new List<Resource>
        {
            new() { ProjectId = null, Name = "项目经理",   Type = ResourceType.Labor,    Quantity = 1,  HourlyCost = 150m,  Notes = "一级建造师",    Unit = "人",   UnitPrice = 0,    CreatedAt = DateTime.UtcNow },
            new() { ProjectId = null, Name = "港航工程师", Type = ResourceType.Labor,    Quantity = 2,  HourlyCost = 100m,  Notes = "中级职称",      Unit = "人",   UnitPrice = 0,    CreatedAt = DateTime.UtcNow },
            new() { ProjectId = null, Name = "挖泥船",     Type = ResourceType.Equipment, Quantity = 1,  HourlyCost = 2000m, Notes = "舱容≥4500m³",  Unit = "艘",   UnitPrice = 0,    CreatedAt = DateTime.UtcNow },
            new() { ProjectId = null, Name = "起重船",     Type = ResourceType.Equipment, Quantity = 1,  HourlyCost = 3000m, Notes = "起吊≥500t",    Unit = "艘",   UnitPrice = 0,    CreatedAt = DateTime.UtcNow },
            new() { ProjectId = null, Name = "混凝土",     Type = ResourceType.Material,  Quantity = 5000, HourlyCost = null, Notes = "C40，单位:m³", Unit = "m³", UnitPrice = 400m, CreatedAt = DateTime.UtcNow },
        };
        db.Resources.AddRange(resources);
        db.SaveChanges();
    }
}
