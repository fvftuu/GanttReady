using GanttReady.Server.Models;

namespace GanttReady.Server.Data;

/// <summary>
/// 示例数据初始化 — 综合基础设施建设项目
/// 30个任务覆盖5个阶段：施工准备→基础工程→主体结构→机电安装→装修收尾
/// </summary>
public static class DataSeeder
{
    public static void SeedDemoData(NetPlanDbContext db)
    {
        const string DEMO_PROJECT_CODE = "HP-2026-001";

        if (db.Projects.Any(p => p.Code == DEMO_PROJECT_CODE))
            return;

        var project = new Project
        {
            Code = DEMO_PROJECT_CODE,
            Name = "综合基础设施建设项目",
            Description = "大型综合基础设施工程，涵盖土建、机电、装修等全周期施工",
            PlanStartDate = new DateTime(2026, 1, 13),
            PlanEndDate   = new DateTime(2027, 3, 31),
            WorkingHoursPerDay = 8,
            WorkdaysPerWeek  = 5,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Projects.Add(project);
        db.SaveChanges();

        // ==============================================================================
        // Resources: 22 resources (10 labor + 5 equipment + 7 material)
        // ==============================================================================
        var resources = new List<Resource>
        {
            // --- 人力资源 (10) ---
            new() { ProjectId = project.Id, Code = "PM-01",   Name = "项目经理",   Type = ResourceType.Labor, Unit = "人", Quantity = 1,  UnitPrice = 0,    HourlyCost = 150m,  Notes = "一级建造师",               CreatedAt = DateTime.UtcNow },
            new() { ProjectId = project.Id, Code = "ENG-CV",  Name = "土建工程师", Type = ResourceType.Labor, Unit = "人", Quantity = 3,  UnitPrice = 0,    HourlyCost = 120m,  Notes = "中级以上职称",             CreatedAt = DateTime.UtcNow },
            new() { ProjectId = project.Id, Code = "ENG-ST",  Name = "结构工程师", Type = ResourceType.Labor, Unit = "人", Quantity = 2,  UnitPrice = 0,    HourlyCost = 130m,  Notes = "注册结构工程师",           CreatedAt = DateTime.UtcNow },
            new() { ProjectId = project.Id, Code = "ENG-ME",  Name = "机电工程师", Type = ResourceType.Labor, Unit = "人", Quantity = 2,  UnitPrice = 0,    HourlyCost = 130m,  Notes = "注册电气/暖通",            CreatedAt = DateTime.UtcNow },
            new() { ProjectId = project.Id, Code = "SFT-01",  Name = "安全员",     Type = ResourceType.Labor, Unit = "人", Quantity = 2,  UnitPrice = 0,    HourlyCost = 80m,   Notes = "持安全C证",               CreatedAt = DateTime.UtcNow },
            new() { ProjectId = project.Id, Code = "SUR-01",  Name = "测量员",     Type = ResourceType.Labor, Unit = "人", Quantity = 2,  UnitPrice = 0,    HourlyCost = 70m,   Notes = "持测量员证",               CreatedAt = DateTime.UtcNow },
            new() { ProjectId = project.Id, Code = "LAB-RB",  Name = "钢筋工",     Type = ResourceType.Labor, Unit = "人", Quantity = 10, UnitPrice = 0,    HourlyCost = 60m,   Notes = "中高级钢筋工",             CreatedAt = DateTime.UtcNow },
            new() { ProjectId = project.Id, Code = "LAB-FW",  Name = "模板工",     Type = ResourceType.Labor, Unit = "人", Quantity = 8,  UnitPrice = 0,    HourlyCost = 55m,   Notes = "木工/铝模工",              CreatedAt = DateTime.UtcNow },
            new() { ProjectId = project.Id, Code = "LAB-WD",  Name = "电焊工",     Type = ResourceType.Labor, Unit = "人", Quantity = 5,  UnitPrice = 0,    HourlyCost = 75m,   Notes = "持焊工操作证",             CreatedAt = DateTime.UtcNow },
            new() { ProjectId = project.Id, Code = "LAB-GN",  Name = "普工",       Type = ResourceType.Labor, Unit = "人", Quantity = 20, UnitPrice = 0,    HourlyCost = 45m,   Notes = "辅助用工",                 CreatedAt = DateTime.UtcNow },

            // --- 设备资源 (5) ---
            new() { ProjectId = project.Id, Code = "EQU-EX",  Name = "挖掘机",     Type = ResourceType.Equipment, Unit = "台", Quantity = 2,  UnitPrice = 0,    HourlyCost = 800m,  Notes = "斗容≥1.2m³, 履带式",     CreatedAt = DateTime.UtcNow },
            new() { ProjectId = project.Id, Code = "EQU-CR",  Name = "塔吊",       Type = ResourceType.Equipment, Unit = "台", Quantity = 2,  UnitPrice = 0,    HourlyCost = 1200m, Notes = "QTZ80型, 臂长≥55m",       CreatedAt = DateTime.UtcNow },
            new() { ProjectId = project.Id, Code = "EQU-CP",  Name = "混凝土泵车", Type = ResourceType.Equipment, Unit = "台", Quantity = 1,  UnitPrice = 0,    HourlyCost = 1500m, Notes = "臂架≥37m",                 CreatedAt = DateTime.UtcNow },
            new() { ProjectId = project.Id, Code = "EQU-DR",  Name = "挖泥船",     Type = ResourceType.Equipment, Unit = "艘", Quantity = 1,  UnitPrice = 0,    HourlyCost = 2000m, Notes = "舱容≥4500m³, 绞吸式",      CreatedAt = DateTime.UtcNow },
            new() { ProjectId = project.Id, Code = "EQU-LC",  Name = "起重船",     Type = ResourceType.Equipment, Unit = "艘", Quantity = 1,  UnitPrice = 0,    HourlyCost = 3000m, Notes = "起吊≥500t, 浮式",          CreatedAt = DateTime.UtcNow },

            // --- 材料资源 (7) ---
            new() { ProjectId = project.Id, Code = "MAT-CON", Name = "混凝土C40",   Type = ResourceType.Material, Unit = "m³",  Quantity = 5000,  UnitPrice = 400m,   HourlyCost = null,  Notes = "商品混凝土, 塌落度180±20",   CreatedAt = DateTime.UtcNow },
            new() { ProjectId = project.Id, Code = "MAT-RB",  Name = "钢筋",        Type = ResourceType.Material, Unit = "t",   Quantity = 800,   UnitPrice = 4500m,  HourlyCost = null,  Notes = "HRB400E, φ8-32mm",          CreatedAt = DateTime.UtcNow },
            new() { ProjectId = project.Id, Code = "MAT-FW",  Name = "模板",        Type = ResourceType.Material, Unit = "m²",  Quantity = 3000,  UnitPrice = 85m,    HourlyCost = null,  Notes = "铝合金模板体系",              CreatedAt = DateTime.UtcNow },
            new() { ProjectId = project.Id, Code = "MAT-CB",  Name = "电缆",        Type = ResourceType.Material, Unit = "m",   Quantity = 5000,  UnitPrice = 120m,   HourlyCost = null,  Notes = "YJV-0.6/1kV铜芯",           CreatedAt = DateTime.UtcNow },
            new() { ProjectId = project.Id, Code = "MAT-PP",  Name = "管材",        Type = ResourceType.Material, Unit = "m",   Quantity = 3000,  UnitPrice = 65m,    HourlyCost = null,  Notes = "PPR/镀锌钢管/PE管综合",       CreatedAt = DateTime.UtcNow },
            new() { ProjectId = project.Id, Code = "MAT-WP",  Name = "防水材料",    Type = ResourceType.Material, Unit = "m²",  Quantity = 2000,  UnitPrice = 55m,    HourlyCost = null,  Notes = "SBS改性沥青+聚氨酯涂膜",      CreatedAt = DateTime.UtcNow },
            new() { ProjectId = project.Id, Code = "MAT-PT",  Name = "涂料",        Type = ResourceType.Material, Unit = "L",   Quantity = 1500,  UnitPrice = 35m,    HourlyCost = null,  Notes = "内外墙乳胶漆, 环保型",        CreatedAt = DateTime.UtcNow },

            // --- 措施类资源 (4) ---
            new() { ProjectId = project.Id, Code = "MEA-SAFE", Name = "安全文明施工费",  Type = ResourceType.Measure, Unit = "项", Quantity = 1,   UnitPrice = 250000m, HourlyCost = null, Notes = "安全防护/警示/文明施工/围挡", CreatedAt = DateTime.UtcNow },
            new() { ProjectId = project.Id, Code = "MEA-TEMP", Name = "临时设施费",    Type = ResourceType.Measure, Unit = "项", Quantity = 1,   UnitPrice = 180000m, HourlyCost = null, Notes = "临时水电/道路/办公板房",   CreatedAt = DateTime.UtcNow },
            new() { ProjectId = project.Id, Code = "MEA-WTR",  Name = "冬雨季施工措施费", Type = ResourceType.Measure, Unit = "项", Quantity = 1,   UnitPrice = 120000m, HourlyCost = null, Notes = "冬季保温/雨季排水/防滑",   CreatedAt = DateTime.UtcNow },
            new() { ProjectId = project.Id, Code = "MEA-PROT", Name = "已完工程保护费",  Type = ResourceType.Measure, Unit = "项", Quantity = 1,   UnitPrice = 80000m,  HourlyCost = null, Notes = "成品保护/覆盖/围栏",     CreatedAt = DateTime.UtcNow },
        };
        db.Resources.AddRange(resources);
        db.SaveChanges();

        // Shortcut references
        var r = resources; // r[0]=PM-01, r[1]=ENG-CV, r[2]=ENG-ST, r[3]=ENG-ME, r[4]=SFT-01,
                           // r[5]=SUR-01, r[6]=LAB-RB, r[7]=LAB-FW, r[8]=LAB-WD, r[9]=LAB-GN,
                           // r[10]=EQU-EX, r[11]=EQU-CR, r[12]=EQU-CP, r[13]=EQU-DR, r[14]=EQU-LC,
                           // r[15]=MAT-CON, r[16]=MAT-RB, r[17]=MAT-FW, r[18]=MAT-CB, r[19]=MAT-PP,
                           // r[20]=MAT-WP, r[21]=MAT-PT, r[22]=MEA-SAFE, r[23]=MEA-TEMP, r[24]=MEA-WTR, r[25]=MEA-PROT

        // ==============================================================================
        // Tasks: 30 tasks across 5 phases (A→E)
        // Key: 已完成(100%)=A1/A3/A4/A5, 进行中=A2(65%)/B1(30%), 其余未开始(0%)
        // ==============================================================================
        var startDate = new DateTime(2026, 1, 1);
        var tasks = new List<TaskItem>
        {
            // ==================== A阶段：施工准备 (1-5) — 大部分已完成 ====================
            new()
            {
                ProjectId = project.Id, Code = "A1", Name = "项目立项与审批", SortOrder = 1,
                ResponsiblePerson = "张建国",
                PlanStartDate = startDate.AddDays(12), PlanEndDate = startDate.AddDays(41), PlanDuration = 30,
                ActualStartDate = startDate.AddDays(12), ActualEndDate = startDate.AddDays(41), ActualDuration = 30,
                CompletionPercentage = 100,
                BudgetCost = 5000m,
                ActualCost = 4800m,
                Notes = "已取得立项批复、用地规划许可证、工程规划许可证" + '\n' + "发改批文号: 发改投资〔2025〕887号",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                ProjectId = project.Id, Code = "A2", Name = "施工图设计", SortOrder = 2,
                ResponsiblePerson = "李明辉",
                PlanStartDate = startDate.AddDays(15), PlanEndDate = startDate.AddDays(134), PlanDuration = 120,
                ActualStartDate = startDate.AddDays(14), ActualEndDate = null, ActualDuration = null,
                CompletionPercentage = 65,
                BudgetCost = 30000m,
                ActualCost = null,
                Notes = "设计院已交付结构/建筑/给排水图纸, 机电/暖通图纸深化中" + '\n' + "设计单位: XX建筑设计研究院",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                ProjectId = project.Id, Code = "A3", Name = "施工现场临建", SortOrder = 3,
                ResponsiblePerson = "王德发",
                PlanStartDate = startDate.AddDays(30), PlanEndDate = startDate.AddDays(59), PlanDuration = 30,
                ActualStartDate = startDate.AddDays(30), ActualEndDate = startDate.AddDays(57), ActualDuration = 28,
                CompletionPercentage = 100,
                BudgetCost = 20000m,
                ActualCost = 21000m,
                Notes = "办公区/生活区板房搭建完成, 临时水电接通, 围挡完成" + '\n' + "临建面积: 办公800m² + 生活1200m²",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                ProjectId = project.Id, Code = "A4", Name = "施工组织设计编制", SortOrder = 4,
                ResponsiblePerson = "赵志强",
                PlanStartDate = startDate.AddDays(30), PlanEndDate = startDate.AddDays(59), PlanDuration = 30,
                ActualStartDate = startDate.AddDays(31), ActualEndDate = startDate.AddDays(60), ActualDuration = 30,
                CompletionPercentage = 100,
                BudgetCost = 8000m,
                ActualCost = 8000m,
                Notes = "施工组织设计已通过监理审批, 专项方案(深基坑/高支模/塔吊)已论证",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                ProjectId = project.Id, Code = "A5", Name = "施工设备进场", SortOrder = 5,
                ResponsiblePerson = "陈大勇",
                PlanStartDate = startDate.AddDays(45), PlanEndDate = startDate.AddDays(74), PlanDuration = 30,
                ActualStartDate = startDate.AddDays(44), ActualEndDate = startDate.AddDays(73), ActualDuration = 30,
                CompletionPercentage = 100,
                BudgetCost = 100000m,
                ActualCost = 105000m,
                Notes = "塔吊2台/挖掘机2台/混凝土泵车1台已进场并验收合格",
                CreatedAt = DateTime.UtcNow
            },

            // ==================== B阶段：基础工程 (6-10) — B1进行中, 其余未开始 ====================
            new()
            {
                ProjectId = project.Id, Code = "B1", Name = "场地平整与大开挖", SortOrder = 6,
                ResponsiblePerson = "王德发",
                PlanStartDate = startDate.AddDays(60), PlanEndDate = startDate.AddDays(149), PlanDuration = 90,
                ActualStartDate = startDate.AddDays(61), ActualEndDate = null, ActualDuration = null,
                CompletionPercentage = 30,
                BudgetCost = 120000m,
                ActualCost = null,
                Notes = "土方开挖量约85000m³, 已完成约26000m³" + '\n' + "开挖深度: 平均-9.5m, 局部-12m",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                ProjectId = project.Id, Code = "B2", Name = "基坑支护工程", SortOrder = 7,
                ResponsiblePerson = "李明辉",
                PlanStartDate = startDate.AddDays(90), PlanEndDate = startDate.AddDays(179), PlanDuration = 90,
                ActualStartDate = null, ActualEndDate = null, ActualDuration = null,
                CompletionPercentage = 0,
                BudgetCost = 80000m,
                Notes = "支护方案: 排桩+内支撑(局部地下连续墙)" + '\n' + "支护深度: 12m, 安全等级一级",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                ProjectId = project.Id, Code = "B3", Name = "桩基施工", SortOrder = 8,
                ResponsiblePerson = "赵志强",
                PlanStartDate = startDate.AddDays(120), PlanEndDate = startDate.AddDays(269), PlanDuration = 150,
                ActualStartDate = null, ActualEndDate = null, ActualDuration = null,
                CompletionPercentage = 0,
                BudgetCost = 150000m,
                Notes = "钻孔灌注桩 φ800mm, 桩长35-42m, 共486根" + '\n' + "持力层: 中风化花岗岩, 单桩承载力特征值≥4500kN",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                ProjectId = project.Id, Code = "B4", Name = "地下室结构", SortOrder = 9,
                ResponsiblePerson = "李明辉",
                PlanStartDate = startDate.AddDays(180), PlanEndDate = startDate.AddDays(359), PlanDuration = 180,
                ActualStartDate = null, ActualEndDate = null, ActualDuration = null,
                CompletionPercentage = 0,
                BudgetCost = 200000m,
                Notes = "地下2层, 面积约12000m²" + '\n' + "筏板基础厚1.8m, C40P8抗渗混凝土" + '\n' + "后浇带/施工缝按设计图留置",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                ProjectId = project.Id, Code = "B5", Name = "防水工程", SortOrder = 10,
                ResponsiblePerson = "陈大勇",
                PlanStartDate = startDate.AddDays(300), PlanEndDate = startDate.AddDays(389), PlanDuration = 90,
                ActualStartDate = null, ActualEndDate = null, ActualDuration = null,
                CompletionPercentage = 0,
                BudgetCost = 100000m,
                Notes = "SBS改性沥青防水卷材+聚氨酯涂膜防水" + '\n' + "防水等级: 一级, 施工面积约15000m²",
                CreatedAt = DateTime.UtcNow
            },

            // ==================== C阶段：主体结构 (11-18) — 全部未开始 ====================
            new()
            {
                ProjectId = project.Id, Code = "C1", Name = "地上结构一层", SortOrder = 11,
                ResponsiblePerson = "赵志强",
                PlanStartDate = startDate.AddDays(330), PlanEndDate = startDate.AddDays(419), PlanDuration = 90,
                ActualStartDate = null, ActualEndDate = null, ActualDuration = null,
                CompletionPercentage = 0,
                BudgetCost = 120000m,
                Notes = "层高5.4m(商业裙楼), 框架-剪力墙结构" + '\n' + "混凝土C40, 钢筋HRB400E" + '\n' + "模板: 铝合金快拆体系",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                ProjectId = project.Id, Code = "C2", Name = "地上结构二层", SortOrder = 12,
                ResponsiblePerson = "赵志强",
                PlanStartDate = startDate.AddDays(360), PlanEndDate = startDate.AddDays(449), PlanDuration = 90,
                ActualStartDate = null, ActualEndDate = null, ActualDuration = null,
                CompletionPercentage = 0,
                BudgetCost = 100000m,
                Notes = "层高4.5m(办公区标准层), 约2200m²/层",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                ProjectId = project.Id, Code = "C3", Name = "地上结构三层", SortOrder = 13,
                ResponsiblePerson = "赵志强",
                PlanStartDate = startDate.AddDays(390), PlanEndDate = startDate.AddDays(479), PlanDuration = 90,
                ActualStartDate = null, ActualEndDate = null, ActualDuration = null,
                CompletionPercentage = 0,
                BudgetCost = 100000m,
                Notes = "标准层, 结构形式同上",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                ProjectId = project.Id, Code = "C4", Name = "地上结构四层", SortOrder = 14,
                ResponsiblePerson = "赵志强",
                PlanStartDate = startDate.AddDays(420), PlanEndDate = startDate.AddDays(509), PlanDuration = 90,
                ActualStartDate = null, ActualEndDate = null, ActualDuration = null,
                CompletionPercentage = 0,
                BudgetCost = 100000m,
                Notes = "标准层, 结构形式同上",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                ProjectId = project.Id, Code = "C5", Name = "地上结构五层", SortOrder = 15,
                ResponsiblePerson = "赵志强",
                PlanStartDate = startDate.AddDays(450), PlanEndDate = startDate.AddDays(539), PlanDuration = 90,
                ActualStartDate = null, ActualEndDate = null, ActualDuration = null,
                CompletionPercentage = 0,
                BudgetCost = 100000m,
                Notes = "顶层, 含屋面设备基础预埋件" + '\n' + "层高4.2m, 局部夹层",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                ProjectId = project.Id, Code = "C6", Name = "主体结构封顶", SortOrder = 16,
                ResponsiblePerson = "张建国",
                PlanStartDate = startDate.AddDays(480), PlanEndDate = startDate.AddDays(549), PlanDuration = 70,
                ActualStartDate = null, ActualEndDate = null, ActualDuration = null,
                IsMilestone = true, CompletionPercentage = 0,
                BudgetCost = 30000m,
                Notes = "里程碑节点 — 主体结构混凝土浇筑全部完成" + '\n' + "封顶仪式后即转入装修安装阶段",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                ProjectId = project.Id, Code = "C7", Name = "屋面工程", SortOrder = 17,
                ResponsiblePerson = "陈大勇",
                PlanStartDate = startDate.AddDays(540), PlanEndDate = startDate.AddDays(629), PlanDuration = 90,
                ActualStartDate = null, ActualEndDate = null, ActualDuration = null,
                CompletionPercentage = 0,
                BudgetCost = 80000m,
                Notes = "找坡层+保温层+防水层+保护层" + '\n' + "上人屋面铺设防滑地砖" + '\n' + "女儿墙高度1.5m, 泛水/天沟按规范施工",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                ProjectId = project.Id, Code = "C8", Name = "外墙围护结构", SortOrder = 18,
                ResponsiblePerson = "李明辉",
                PlanStartDate = startDate.AddDays(550), PlanEndDate = startDate.AddDays(729), PlanDuration = 180,
                ActualStartDate = null, ActualEndDate = null, ActualDuration = null,
                CompletionPercentage = 0,
                BudgetCost = 180000m,
                Notes = "玻璃幕墙+石材幕墙+铝板幕墙组合" + '\n' + "幕墙面积约18000m², 四性试验已通过",
                CreatedAt = DateTime.UtcNow
            },

            // ==================== D阶段：机电安装 (19-24) — 全部未开始 ====================
            new()
            {
                ProjectId = project.Id, Code = "D1", Name = "电气系统安装", SortOrder = 19,
                ResponsiblePerson = "钱学林",
                PlanStartDate = startDate.AddDays(450), PlanEndDate = startDate.AddDays(809), PlanDuration = 360,
                ActualStartDate = null, ActualEndDate = null, ActualDuration = null,
                CompletionPercentage = 0,
                BudgetCost = 200000m,
                Notes = "高压配电10kV→低压0.4kV" + '\n' + "变压器2×1250kVA, 配电柜48面" + '\n' + "桥架/线管约12000m, 电缆约18000m",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                ProjectId = project.Id, Code = "D2", Name = "给排水系统", SortOrder = 20,
                ResponsiblePerson = "周建国",
                PlanStartDate = startDate.AddDays(450), PlanEndDate = startDate.AddDays(779), PlanDuration = 330,
                ActualStartDate = null, ActualEndDate = null, ActualDuration = null,
                CompletionPercentage = 0,
                BudgetCost = 120000m,
                Notes = "生活给水+消防给水+污废水排放" + '\n' + "水泵房1座, 水箱容积200m³" + '\n' + "PPR给水管+UPVC排水管, 管径DN15-DN200",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                ProjectId = project.Id, Code = "D3", Name = "通风与空调", SortOrder = 21,
                ResponsiblePerson = "钱学林",
                PlanStartDate = startDate.AddDays(500), PlanEndDate = startDate.AddDays(839), PlanDuration = 340,
                ActualStartDate = null, ActualEndDate = null, ActualDuration = null,
                CompletionPercentage = 0,
                BudgetCost = 180000m,
                Notes = "集中冷源: 水冷螺杆机组2台×1200RT" + '\n' + "AHU+FCU系统, 风管总面积约15000m²" + '\n' + "排烟风机/正压送风机共28台",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                ProjectId = project.Id, Code = "D4", Name = "消防系统", SortOrder = 22,
                ResponsiblePerson = "周建国",
                PlanStartDate = startDate.AddDays(550), PlanEndDate = startDate.AddDays(869), PlanDuration = 320,
                ActualStartDate = null, ActualEndDate = null, ActualDuration = null,
                CompletionPercentage = 0,
                BudgetCost = 150000m,
                Notes = "自动喷淋(湿式)+消火栓+气体灭火(变配电室)" + '\n' + "火灾自动报警: 烟感1260点, 手报85个" + '\n' + "消防泵2台, 稳压装置1套",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                ProjectId = project.Id, Code = "D5", Name = "弱电智能化系统", SortOrder = 23,
                ResponsiblePerson = "钱学林",
                PlanStartDate = startDate.AddDays(600), PlanEndDate = startDate.AddDays(959), PlanDuration = 360,
                ActualStartDate = null, ActualEndDate = null, ActualDuration = null,
                CompletionPercentage = 0,
                BudgetCost = 220000m,
                Notes = "综合布线: 信息点1560个, 光纤主干+六类铜缆" + '\n' + "安防: 视频监控(IP1080P)×260路, 门禁128点" + '\n' + "楼宇自控(BAS)/智能照明/能耗监测",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                ProjectId = project.Id, Code = "D6", Name = "电梯安装", SortOrder = 24,
                ResponsiblePerson = "陈大勇",
                PlanStartDate = startDate.AddDays(700), PlanEndDate = startDate.AddDays(989), PlanDuration = 290,
                ActualStartDate = null, ActualEndDate = null, ActualDuration = null,
                CompletionPercentage = 0,
                BudgetCost = 100000m,
                Notes = "客梯4台(1.75m/s, 1350kg)+货梯1台(1.0m/s, 2000kg)" + '\n' + "品牌: 通过公开招标确定, 安装后需特检院验收",
                CreatedAt = DateTime.UtcNow
            },

            // ==================== E阶段：装修与收尾 (25-30) — 全部未开始 ====================
            new()
            {
                ProjectId = project.Id, Code = "E1", Name = "室内装修工程", SortOrder = 25,
                ResponsiblePerson = "李明辉",
                PlanStartDate = startDate.AddDays(730), PlanEndDate = startDate.AddDays(989), PlanDuration = 260,
                ActualStartDate = null, ActualEndDate = null, ActualDuration = null,
                CompletionPercentage = 0,
                BudgetCost = 150000m,
                Notes = "精装修面积约15000m²(公共区域+办公区)" + '\n' + "墙顶地: 乳胶漆+铝扣板+玻化砖/地毯" + '\n' + "卫生间/茶水间防水先行",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                ProjectId = project.Id, Code = "E2", Name = "景观绿化工程", SortOrder = 26,
                ResponsiblePerson = "王德发",
                PlanStartDate = startDate.AddDays(800), PlanEndDate = startDate.AddDays(1019), PlanDuration = 220,
                ActualStartDate = null, ActualEndDate = null, ActualDuration = null,
                CompletionPercentage = 0,
                BudgetCost = 80000m,
                Notes = "绿化面积约6000m², 乔木120株+灌木3000株+草坪4000m²" + '\n' + "景观水景1处, 休闲广场2处, 园路约800m" + '\n' + "绿化率≥30%",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                ProjectId = project.Id, Code = "E3", Name = "室外管网工程", SortOrder = 27,
                ResponsiblePerson = "周建国",
                PlanStartDate = startDate.AddDays(750), PlanEndDate = startDate.AddDays(929), PlanDuration = 180,
                ActualStartDate = null, ActualEndDate = null, ActualDuration = null,
                CompletionPercentage = 0,
                BudgetCost = 100000m,
                Notes = "给水/消防/污水/雨水/燃气/热力6类管线" + '\n' + "综合管沟长度约1200m" + '\n' + "与市政接口共9处需逐一对接",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                ProjectId = project.Id, Code = "E4", Name = "道路与广场", SortOrder = 28,
                ResponsiblePerson = "王德发",
                PlanStartDate = startDate.AddDays(850), PlanEndDate = startDate.AddDays(1019), PlanDuration = 170,
                ActualStartDate = null, ActualEndDate = null, ActualDuration = null,
                CompletionPercentage = 0,
                BudgetCost = 90000m,
                Notes = "沥青路面约3500m², 广场铺装约2000m²" + '\n' + "路基压实度≥95%, 面层AC-13C细粒式4cm" + '\n' + "停车位128个(含充电桩20个)",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                ProjectId = project.Id, Code = "E5", Name = "竣工验收准备", SortOrder = 29,
                ResponsiblePerson = "赵志强",
                PlanStartDate = startDate.AddDays(980), PlanEndDate = startDate.AddDays(1049), PlanDuration = 70,
                ActualStartDate = null, ActualEndDate = null, ActualDuration = null,
                CompletionPercentage = 0,
                BudgetCost = 30000m,
                Notes = "竣工资料归档(含施工日志/隐蔽验收/检测报告/竣工图)" + '\n' + "各系统联调联试, 问题整改销项" + '\n' + "消防/环保/规划/人防专项验收",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                ProjectId = project.Id, Code = "E6", Name = "竣工验收交付", SortOrder = 30,
                ResponsiblePerson = "张建国",
                PlanStartDate = startDate.AddDays(1050), PlanEndDate = startDate.AddDays(1094), PlanDuration = 45,
                ActualStartDate = null, ActualEndDate = null, ActualDuration = null,
                IsMilestone = true, CompletionPercentage = 0,
                BudgetCost = 20000m,
                Notes = "里程碑节点 — 五方责任主体验收签章" + '\n' + "移交物业, 质保期起算" + '\n' + "竣工备案/不动产权籍调查同步启动",
                CreatedAt = DateTime.UtcNow
            },
        };
        db.Tasks.AddRange(tasks);
        db.SaveChanges();

        // ==============================================================================
        // Task Relations: 32条前置关系
        // ==============================================================================
        var relations = new List<TaskRelation>
        {
            // A阶段内部
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[0].Id, SuccessorTaskId = tasks[1].Id, Type = RelationType.FS, Lag = 0  }, // A1→A2
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[0].Id, SuccessorTaskId = tasks[2].Id, Type = RelationType.FS, Lag = 0  }, // A1→A3
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[0].Id, SuccessorTaskId = tasks[3].Id, Type = RelationType.FS, Lag = 0  }, // A1→A4
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[1].Id, SuccessorTaskId = tasks[3].Id, Type = RelationType.FS, Lag = 0  }, // A2→A4
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[2].Id, SuccessorTaskId = tasks[4].Id, Type = RelationType.FS, Lag = 0  }, // A3→A5
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[3].Id, SuccessorTaskId = tasks[4].Id, Type = RelationType.FS, Lag = 0  }, // A4→A5

            // A→B
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[4].Id, SuccessorTaskId = tasks[5].Id, Type = RelationType.FS, Lag = 0  }, // A5→B1

            // B阶段内部
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[5].Id, SuccessorTaskId = tasks[6].Id, Type = RelationType.FS, Lag = 15 }, // B1→B2(搭接)
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[6].Id, SuccessorTaskId = tasks[7].Id, Type = RelationType.FS, Lag = 0  }, // B2→B3
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[7].Id, SuccessorTaskId = tasks[8].Id, Type = RelationType.FS, Lag = 30 }, // B3→B4(养护搭接)
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[8].Id, SuccessorTaskId = tasks[9].Id, Type = RelationType.FS, Lag = 30 }, // B4→B5(搭接)

            // B→C
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[9].Id, SuccessorTaskId = tasks[10].Id, Type = RelationType.FS, Lag = 0  }, // B5→C1

            // C阶段内部 (流水施工)
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[10].Id, SuccessorTaskId = tasks[11].Id, Type = RelationType.FS, Lag = 0 }, // C1→C2
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[11].Id, SuccessorTaskId = tasks[12].Id, Type = RelationType.FS, Lag = 0 }, // C2→C3
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[12].Id, SuccessorTaskId = tasks[13].Id, Type = RelationType.FS, Lag = 0 }, // C3→C4
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[13].Id, SuccessorTaskId = tasks[14].Id, Type = RelationType.FS, Lag = 0 }, // C4→C5
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[14].Id, SuccessorTaskId = tasks[15].Id, Type = RelationType.FS, Lag = 0 }, // C5→C6

            // C6→后续 (主体封顶后多条并行)
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[15].Id, SuccessorTaskId = tasks[16].Id, Type = RelationType.FS, Lag = 30 }, // C6→C7
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[15].Id, SuccessorTaskId = tasks[17].Id, Type = RelationType.FS, Lag = 0  }, // C6→C8(立即开始)
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[15].Id, SuccessorTaskId = tasks[18].Id, Type = RelationType.FS, Lag = 0  }, // C6→D1
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[15].Id, SuccessorTaskId = tasks[19].Id, Type = RelationType.FS, Lag = 0  }, // C6→D2
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[15].Id, SuccessorTaskId = tasks[20].Id, Type = RelationType.FS, Lag = 20 }, // C6→D3
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[15].Id, SuccessorTaskId = tasks[21].Id, Type = RelationType.FS, Lag = 30 }, // C6→D4

            // D阶段内部
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[18].Id, SuccessorTaskId = tasks[22].Id, Type = RelationType.FS, Lag = 60 }, // D1→D5
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[21].Id, SuccessorTaskId = tasks[22].Id, Type = RelationType.FS, Lag = 0  }, // D4→D5
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[18].Id, SuccessorTaskId = tasks[23].Id, Type = RelationType.FS, Lag = 150}, // D1→D6

            // C→E
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[17].Id, SuccessorTaskId = tasks[24].Id, Type = RelationType.FS, Lag = 60 }, // C8→E1(幕墙完成→装修)
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[17].Id, SuccessorTaskId = tasks[26].Id, Type = RelationType.FS, Lag = 60 }, // C8→E3(幕墙完成→室外管网)

            // E阶段内部
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[26].Id, SuccessorTaskId = tasks[25].Id, Type = RelationType.FS, Lag = 30 }, // E3→E2(管网→绿化)
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[26].Id, SuccessorTaskId = tasks[27].Id, Type = RelationType.FS, Lag = 30 }, // E3→E4(管网→道路)

            // D+E→收尾
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[24].Id, SuccessorTaskId = tasks[28].Id, Type = RelationType.FS, Lag = 0  }, // E1→E5
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[27].Id, SuccessorTaskId = tasks[28].Id, Type = RelationType.FS, Lag = 0  }, // E4→E5

            // 收尾
            new() { ProjectId = project.Id, PredecessorTaskId = tasks[28].Id, SuccessorTaskId = tasks[29].Id, Type = RelationType.FS, Lag = 0  }, // E5→E6
        };
        db.TaskRelations.AddRange(relations);
        db.SaveChanges();

        // ==============================================================================
        // Resource Assignments: 任务-资源关联 (每个任务分配1-4个资源)
        // ==============================================================================
        var assignments = new List<ResourceAssignment>
        {
            // A1 项目立项与审批: 项目经理 + 措施费
            new() { TaskId = tasks[0].Id, ResourceId = r[0].Id, Quantity = 1, Notes = "全程负责审批对接" },
            new() { TaskId = tasks[0].Id, ResourceId = r[22].Id, Quantity = 1, Notes = "安全文明施工措施费" },
            new() { TaskId = tasks[0].Id, ResourceId = r[23].Id, Quantity = 1, Notes = "临时设施搭建费" },
            new() { TaskId = tasks[0].Id, ResourceId = r[24].Id, Quantity = 1, Notes = "冬雨季施工措施费" },
            new() { TaskId = tasks[0].Id, ResourceId = r[25].Id, Quantity = 1, Notes = "已完工程保护费" },

            // A2 施工图设计: 土建/结构工程师
            new() { TaskId = tasks[1].Id, ResourceId = r[1].Id, Quantity = 1, Notes = "建筑专业设计协调" },
            new() { TaskId = tasks[1].Id, ResourceId = r[2].Id, Quantity = 1, Notes = "结构专业设计审核" },

            // A3 施工现场临建: 土建工程师+模板工+普工
            new() { TaskId = tasks[2].Id, ResourceId = r[1].Id, Quantity = 1, Notes = "临建方案编制与实施监督" },
            new() { TaskId = tasks[2].Id, ResourceId = r[7].Id, Quantity = 3, Notes = "板房/围挡搭建" },
            new() { TaskId = tasks[2].Id, ResourceId = r[9].Id, Quantity = 10, Notes = "场地硬化/临时水电" },

            // A4 施工组织设计编制: 项目经理+安全员
            new() { TaskId = tasks[3].Id, ResourceId = r[0].Id, Quantity = 1, Notes = "总体策划与审批" },
            new() { TaskId = tasks[3].Id, ResourceId = r[4].Id, Quantity = 1, Notes = "安全专项方案" },

            // A5 施工设备进场: 安全员+普工+起重船
            new() { TaskId = tasks[4].Id, ResourceId = r[4].Id, Quantity = 1, Notes = "设备进场验收" },
            new() { TaskId = tasks[4].Id, ResourceId = r[9].Id, Quantity = 6, Notes = "设备卸车/安装配合" },
            new() { TaskId = tasks[4].Id, ResourceId = r[14].Id, Quantity = 1, Notes = "起重船配合转运大型设备" },

            // B1 场地平整与大开挖: 土建工程师+挖掘机+普工
            new() { TaskId = tasks[5].Id, ResourceId = r[1].Id, Quantity = 1, Notes = "土方施工管理" },
            new() { TaskId = tasks[5].Id, ResourceId = r[10].Id, Quantity = 2, Notes = "土方开挖" },
            new() { TaskId = tasks[5].Id, ResourceId = r[9].Id, Quantity = 8, Notes = "配合清槽/修坡" },

            // B2 基坑支护工程: 结构工程师+钢筋工+混凝土
            new() { TaskId = tasks[6].Id, ResourceId = r[2].Id, Quantity = 1, Notes = "支护方案审核与实施" },
            new() { TaskId = tasks[6].Id, ResourceId = r[6].Id, Quantity = 5, Notes = "支护桩/内支撑钢筋" },
            new() { TaskId = tasks[6].Id, ResourceId = r[15].Id, Quantity = 300, Notes = "支护桩/冠梁混凝土" },

            // B3 桩基施工: 结构工程师+挖泥船+混凝土+电焊工
            new() { TaskId = tasks[7].Id, ResourceId = r[2].Id, Quantity = 1, Notes = "桩基施工管理" },
            new() { TaskId = tasks[7].Id, ResourceId = r[13].Id, Quantity = 1, Notes = "水上桩基施工配合" },
            new() { TaskId = tasks[7].Id, ResourceId = r[15].Id, Quantity = 3500, Notes = "灌注桩混凝土" },
            new() { TaskId = tasks[7].Id, ResourceId = r[8].Id, Quantity = 4, Notes = "钢筋笼焊接" },

            // B4 地下室结构: 结构工程师+钢筋工+模板工+混凝土+塔吊
            new() { TaskId = tasks[8].Id, ResourceId = r[2].Id, Quantity = 1, Notes = "地下室结构施工管理" },
            new() { TaskId = tasks[8].Id, ResourceId = r[6].Id, Quantity = 10, Notes = "底板/墙/柱/顶板钢筋" },
            new() { TaskId = tasks[8].Id, ResourceId = r[7].Id, Quantity = 8, Notes = "模板支设与拆除" },
            new() { TaskId = tasks[8].Id, ResourceId = r[15].Id, Quantity = 2000, Notes = "筏板+墙+顶板混凝土" },
            new() { TaskId = tasks[8].Id, ResourceId = r[11].Id, Quantity = 1, Notes = "材料垂直运输" },

            // B5 防水工程: 土建工程师+防水材料+普工
            new() { TaskId = tasks[9].Id, ResourceId = r[1].Id, Quantity = 1, Notes = "防水施工管理" },
            new() { TaskId = tasks[9].Id, ResourceId = r[20].Id, Quantity = 1500, Notes = "卷材+涂膜" },
            new() { TaskId = tasks[9].Id, ResourceId = r[9].Id, Quantity = 6, Notes = "防水施工" },

            // C1-C5 地上结构1-5层: 结构工程师+钢筋工+模板工+混凝土+塔吊+测量员
            new() { TaskId = tasks[10].Id, ResourceId = r[2].Id, Quantity = 1, Notes = "结构施工管理" },
            new() { TaskId = tasks[10].Id, ResourceId = r[6].Id, Quantity = 8, Notes = "梁板柱钢筋" },
            new() { TaskId = tasks[10].Id, ResourceId = r[7].Id, Quantity = 6, Notes = "铝合金模板支设" },
            new() { TaskId = tasks[10].Id, ResourceId = r[15].Id, Quantity = 400, Notes = "梁板柱混凝土" },
            new() { TaskId = tasks[10].Id, ResourceId = r[11].Id, Quantity = 1, Notes = "垂直运输" },
            new() { TaskId = tasks[10].Id, ResourceId = r[5].Id, Quantity = 1, Notes = "放线/标高控制" },

            new() { TaskId = tasks[11].Id, ResourceId = r[2].Id, Quantity = 1 },
            new() { TaskId = tasks[11].Id, ResourceId = r[6].Id, Quantity = 7, Notes = "二层钢筋" },
            new() { TaskId = tasks[11].Id, ResourceId = r[7].Id, Quantity = 5, Notes = "二层模板" },
            new() { TaskId = tasks[11].Id, ResourceId = r[15].Id, Quantity = 350, Notes = "二层混凝土" },
            new() { TaskId = tasks[11].Id, ResourceId = r[11].Id, Quantity = 1 },

            new() { TaskId = tasks[12].Id, ResourceId = r[2].Id, Quantity = 1 },
            new() { TaskId = tasks[12].Id, ResourceId = r[6].Id, Quantity = 7 },
            new() { TaskId = tasks[12].Id, ResourceId = r[7].Id, Quantity = 5 },
            new() { TaskId = tasks[12].Id, ResourceId = r[15].Id, Quantity = 350 },
            new() { TaskId = tasks[12].Id, ResourceId = r[11].Id, Quantity = 1 },

            new() { TaskId = tasks[13].Id, ResourceId = r[2].Id, Quantity = 1 },
            new() { TaskId = tasks[13].Id, ResourceId = r[6].Id, Quantity = 7 },
            new() { TaskId = tasks[13].Id, ResourceId = r[7].Id, Quantity = 5 },
            new() { TaskId = tasks[13].Id, ResourceId = r[15].Id, Quantity = 350 },
            new() { TaskId = tasks[13].Id, ResourceId = r[11].Id, Quantity = 1 },

            new() { TaskId = tasks[14].Id, ResourceId = r[2].Id, Quantity = 1 },
            new() { TaskId = tasks[14].Id, ResourceId = r[6].Id, Quantity = 7 },
            new() { TaskId = tasks[14].Id, ResourceId = r[7].Id, Quantity = 5 },
            new() { TaskId = tasks[14].Id, ResourceId = r[15].Id, Quantity = 350 },
            new() { TaskId = tasks[14].Id, ResourceId = r[11].Id, Quantity = 1 },

            // C6 主体结构封顶: 项目经理+结构工程师 (里程碑)
            new() { TaskId = tasks[15].Id, ResourceId = r[0].Id, Quantity = 1, Notes = "主持封顶节点验收" },
            new() { TaskId = tasks[15].Id, ResourceId = r[2].Id, Quantity = 1, Notes = "结构封顶质量核查" },

            // C7 屋面工程: 土建工程师+防水材料+普工
            new() { TaskId = tasks[16].Id, ResourceId = r[1].Id, Quantity = 1, Notes = "屋面施工管理" },
            new() { TaskId = tasks[16].Id, ResourceId = r[20].Id, Quantity = 500, Notes = "屋面防水" },
            new() { TaskId = tasks[16].Id, ResourceId = r[9].Id, Quantity = 5, Notes = "屋面施工" },

            // C8 外墙围护结构: 土建工程师+模板工+电焊工+塔吊
            new() { TaskId = tasks[17].Id, ResourceId = r[1].Id, Quantity = 1, Notes = "幕墙施工管理" },
            new() { TaskId = tasks[17].Id, ResourceId = r[7].Id, Quantity = 4, Notes = "幕墙龙骨/预埋件" },
            new() { TaskId = tasks[17].Id, ResourceId = r[8].Id, Quantity = 5, Notes = "幕墙钢结构焊接" },
            new() { TaskId = tasks[17].Id, ResourceId = r[11].Id, Quantity = 1, Notes = "幕墙板块吊装" },

            // D1 电气系统安装: 机电工程师+电缆+普工
            new() { TaskId = tasks[18].Id, ResourceId = r[3].Id, Quantity = 1, Notes = "电气施工管理" },
            new() { TaskId = tasks[18].Id, ResourceId = r[18].Id, Quantity = 3000, Notes = "电缆敷设" },
            new() { TaskId = tasks[18].Id, ResourceId = r[9].Id, Quantity = 6, Notes = "桥架/配管安装" },

            // D2 给排水系统: 机电工程师+管材+普工
            new() { TaskId = tasks[19].Id, ResourceId = r[3].Id, Quantity = 1, Notes = "给排水施工管理" },
            new() { TaskId = tasks[19].Id, ResourceId = r[19].Id, Quantity = 1500, Notes = "给水管/排水管" },
            new() { TaskId = tasks[19].Id, ResourceId = r[9].Id, Quantity = 5, Notes = "管道安装" },

            // D3 通风与空调: 机电工程师+管材+电焊工+塔吊
            new() { TaskId = tasks[20].Id, ResourceId = r[3].Id, Quantity = 1, Notes = "暖通施工管理" },
            new() { TaskId = tasks[20].Id, ResourceId = r[19].Id, Quantity = 800, Notes = "风管材料" },
            new() { TaskId = tasks[20].Id, ResourceId = r[8].Id, Quantity = 4, Notes = "风管焊接/法兰连接" },
            new() { TaskId = tasks[20].Id, ResourceId = r[11].Id, Quantity = 1, Notes = "设备吊装" },

            // D4 消防系统: 机电工程师+管材+电焊工
            new() { TaskId = tasks[21].Id, ResourceId = r[3].Id, Quantity = 1, Notes = "消防施工管理" },
            new() { TaskId = tasks[21].Id, ResourceId = r[19].Id, Quantity = 700, Notes = "消防管道" },
            new() { TaskId = tasks[21].Id, ResourceId = r[8].Id, Quantity = 3, Notes = "管道焊接" },

            // D5 弱电智能化系统: 机电工程师+电缆+普工
            new() { TaskId = tasks[22].Id, ResourceId = r[3].Id, Quantity = 1, Notes = "弱电施工管理" },
            new() { TaskId = tasks[22].Id, ResourceId = r[18].Id, Quantity = 2000, Notes = "综合布线线缆" },
            new() { TaskId = tasks[22].Id, ResourceId = r[9].Id, Quantity = 5, Notes = "线管/桥架安装" },

            // D6 电梯安装: 机电工程师+塔吊+普工
            new() { TaskId = tasks[23].Id, ResourceId = r[3].Id, Quantity = 1, Notes = "电梯安装协调" },
            new() { TaskId = tasks[23].Id, ResourceId = r[11].Id, Quantity = 1, Notes = "电梯部件吊装" },
            new() { TaskId = tasks[23].Id, ResourceId = r[9].Id, Quantity = 4, Notes = "安装配合" },

            // E1 室内装修工程: 土建工程师+涂料+普工
            new() { TaskId = tasks[24].Id, ResourceId = r[1].Id, Quantity = 1, Notes = "装修施工管理" },
            new() { TaskId = tasks[24].Id, ResourceId = r[21].Id, Quantity = 1000, Notes = "内外墙涂料" },
            new() { TaskId = tasks[24].Id, ResourceId = r[9].Id, Quantity = 15, Notes = "装修施工" },

            // E2 景观绿化工程: 土建工程师+普工
            new() { TaskId = tasks[25].Id, ResourceId = r[1].Id, Quantity = 1, Notes = "景观施工管理" },
            new() { TaskId = tasks[25].Id, ResourceId = r[9].Id, Quantity = 8, Notes = "绿化种植/景观施工" },

            // E3 室外管网工程: 机电工程师+管材+普工
            new() { TaskId = tasks[26].Id, ResourceId = r[3].Id, Quantity = 1, Notes = "室外管网施工管理" },
            new() { TaskId = tasks[26].Id, ResourceId = r[19].Id, Quantity = 1000, Notes = "室外各类管道" },
            new() { TaskId = tasks[26].Id, ResourceId = r[9].Id, Quantity = 6, Notes = "管道安装/管沟施工" },

            // E4 道路与广场: 土建工程师+混凝土+挖掘机+测量员
            new() { TaskId = tasks[27].Id, ResourceId = r[1].Id, Quantity = 1, Notes = "道路施工管理" },
            new() { TaskId = tasks[27].Id, ResourceId = r[15].Id, Quantity = 200, Notes = "道路/广场混凝土" },
            new() { TaskId = tasks[27].Id, ResourceId = r[10].Id, Quantity = 1, Notes = "路基整平" },
            new() { TaskId = tasks[27].Id, ResourceId = r[5].Id, Quantity = 1, Notes = "道路放线/标高" },

            // E5 竣工验收准备: 项目经理+安全员
            new() { TaskId = tasks[28].Id, ResourceId = r[0].Id, Quantity = 1, Notes = "竣工验收统筹" },
            new() { TaskId = tasks[28].Id, ResourceId = r[4].Id, Quantity = 1, Notes = "安全评价/消防检测" },

            // E6 竣工验收交付: 项目经理 (里程碑)
            new() { TaskId = tasks[29].Id, ResourceId = r[0].Id, Quantity = 1, Notes = "主持竣工验收会议" },
        };
        db.ResourceAssignments.AddRange(assignments);
        db.SaveChanges();
    }
}
