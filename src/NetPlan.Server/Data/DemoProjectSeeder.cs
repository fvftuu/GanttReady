using NetPlan.Server.Models;

namespace NetPlan.Server.Data;

/// <summary>
/// 多行业演示项目种子数据
/// 7 个不同行业项目，每个含 12-18 个代表性任务
/// </summary>
public static class DemoProjectSeeder
{
    public static void Seed(NetPlanDbContext db)
    {
        var demoData = GetDemoProjects();
        foreach (var (project, tasks, resources, assignments) in demoData)
        {
            if (db.Projects.Any(p => p.Code == project.Code))
                continue;

            db.Projects.Add(project);
            db.SaveChanges();

            // 添加资源
            foreach (var r in resources)
            {
                r.ProjectId = project.Id;
                db.Resources.Add(r);
            }
            db.SaveChanges();
            var resMap = resources.ToDictionary(r => r.Code, r => r);
            // 刷新 resource IDs
            foreach (var r in db.Resources.Where(r => r.ProjectId == project.Id).ToList())
                resMap[r.Code] = r;

            // 添加任务
            var sortOrder = 1;
            var taskList = new List<TaskItem>();
            foreach (var td in tasks)
            {
                var task = new TaskItem
                {
                    ProjectId = project.Id,
                    Code = td.Code ?? $"T-{sortOrder:D2}",
                    Name = td.Name,
                    SortOrder = sortOrder++,
                    ResponsiblePerson = td.Responsible,
                    PlanStartDate = td.Start,
                    PlanEndDate = td.Start.AddDays(td.Duration - 1),
                    PlanDuration = td.Duration,
                    CompletionPercentage = td.Completion,
                    BudgetCost = td.Budget,
                    IsMilestone = td.IsMilestone,
                    Notes = td.Notes,
                    CreatedAt = DateTime.UtcNow
                };
                db.Tasks.Add(task);
                taskList.Add(task);
            }
            db.SaveChanges();

            // 添加资源分配
            if (assignments != null)
            {
                foreach (var (taskIdx, resCode, qty, notes) in assignments)
                {
                    if (taskIdx < taskList.Count && resMap.TryGetValue(resCode, out var res))
                    {
                        db.ResourceAssignments.Add(new ResourceAssignment
                        {
                            TaskId = taskList[taskIdx].Id,
                            ResourceId = res.Id,
                            Quantity = qty,
                            Notes = notes
                        });
                    }
                }
                db.SaveChanges();
            }

            // 按行业真实关系结构添加紧前关系
            var relPlan = GetIndustryRelationPlan(project.Code, taskList);
            foreach (var (predIdx, succIdx, lag) in relPlan)
            {
                if (predIdx < taskList.Count && succIdx < taskList.Count)
                {
                    db.TaskRelations.Add(new TaskRelation
                    {
                        ProjectId = project.Id,
                        PredecessorTaskId = taskList[predIdx].Id,
                        SuccessorTaskId = taskList[succIdx].Id,
                        Type = RelationType.FS,
                        Lag = lag
                    });
                }
            }
            if (relPlan.Any())
                db.SaveChanges();

            // MFR 项目：MR-14 量产→MR-15 QC 质检 改为 SS（量产开始即开始质检）
            if (project.Code == "MFR-2026-001" && relPlan.Any(r => r.succIdx == 14))
            {
                var mfrRel = db.TaskRelations.FirstOrDefault(r =>
                    r.PredecessorTaskId == taskList[13].Id &&
                    r.SuccessorTaskId == taskList[14].Id);
                if (mfrRel != null)
                {
                    mfrRel.Type = RelationType.SS;
                    mfrRel.Lag = 14; // 量产开始后 14 天开始质检（约 9 月下旬）
                }
            }
        }

        // ===== 全局去重：同名资源只保留一个 =====
        // 分组按 (ProjectId, Name)，保留 Quantity 最大的那个
        var allResources = db.Resources.ToList();
        var dupGroups = allResources
            .GroupBy(r => new { r.ProjectId, r.Name })
            .Where(g => g.Count() > 1)
            .ToList();
        foreach (var group in dupGroups)
        {
            // 保留 Quantity 最大的，删除其余
            var keep = group.OrderByDescending(r => r.Quantity).First();
            foreach (var dup in group.Where(r => r.Id != keep.Id))
            {
                // 先解除资源分配引用，再删除
                var assignmentsToRemove = db.ResourceAssignments.Where(a => a.ResourceId == dup.Id).ToList();
                db.ResourceAssignments.RemoveRange(assignmentsToRemove);
                db.Resources.Remove(dup);
            }
        }
        if (dupGroups.Any())
            db.SaveChanges();

        // ===== 补充材料/设备/措施资源 + 自动分配 =====
        AddSupplementaryResourcesAndAssignments(db);
    }

    private static void AddSupplementaryResourcesAndAssignments(NetPlanDbContext db)
    {
        // 每个行业需要补充的材料/设备/措施资源
        var extraResources = new Dictionary<string, List<Resource>>
        {
            ["IT-"] = new()
            {
                new() { Code = "IT-MAT-CLOUD", Name = "云服务资源", Type = ResourceType.Material, Unit = "套", Quantity = 1, UnitPrice = 50000m, Notes = "阿里云ECS/RDS/Redis按年订阅" },
                new() { Code = "IT-MAT-LIC", Name = "软件授权", Type = ResourceType.Material, Unit = "套", Quantity = 1, UnitPrice = 80000m, Notes = "第三方SDK/API/开发工具授权" },
                new() { Code = "IT-EQU-CICD", Name = "CI/CD流水线", Type = ResourceType.Equipment, Unit = "套", Quantity = 1, HourlyCost = 10m, Notes = "Jenkins/GitLab CI/自动化部署" },
                new() { Code = "IT-EQU-MON", Name = "监控告警平台", Type = ResourceType.Equipment, Unit = "套", Quantity = 1, HourlyCost = 5m, Notes = "Prometheus/Grafana/APM" },
                new() { Code = "IT-MEA-SEC", Name = "安全等保测评", Type = ResourceType.Measure, Unit = "项", Quantity = 1, UnitPrice = 150000m, Notes = "等保三级测评+安全整改" },
                new() { Code = "IT-MEA-BAK", Name = "数据备份容灾", Type = ResourceType.Measure, Unit = "套", Quantity = 1, UnitPrice = 30000m, Notes = "异地备份+灾备演练" },
            },
            ["MFR-"] = new()
            {
                new() { Code = "MFR-MAT-PCB", Name = "PCB电路板", Type = ResourceType.Material, Unit = "片", Quantity = 5000, UnitPrice = 15m, Notes = "4层FR-4, 沉金工艺" },
                new() { Code = "MFR-MAT-BATT", Name = "锂电池组", Type = ResourceType.Material, Unit = "个", Quantity = 5000, UnitPrice = 80m, Notes = "18650电芯, 5200mAh" },
                new() { Code = "MFR-MAT-SENSOR", Name = "传感器模组", Type = ResourceType.Material, Unit = "套", Quantity = 5000, UnitPrice = 45m, Notes = "LDS激光雷达+红外+超声" },
                new() { Code = "MFR-MAT-PACK", Name = "包装材料", Type = ResourceType.Material, Unit = "套", Quantity = 50000, UnitPrice = 12m, Notes = "彩盒+内托+说明书" },
                new() { Code = "MFR-EQU-SMT", Name = "SMT贴片线", Type = ResourceType.Equipment, Unit = "条", Quantity = 2, HourlyCost = 600m, Notes = "松下NPM-D3贴片机+回流焊" },
                new() { Code = "MFR-EQU-INJECT", Name = "注塑机", Type = ResourceType.Equipment, Unit = "台", Quantity = 8, HourlyCost = 200m, Notes = "海天MA1600, 锁模力1600T" },
                new() { Code = "MFR-EQU-ASSEM", Name = "总装流水线", Type = ResourceType.Equipment, Unit = "条", Quantity = 2, HourlyCost = 300m, Notes = "环形倍速链, 工位18个/线" },
                new() { Code = "MFR-EQU-TEST", Name = "自动测试设备", Type = ResourceType.Equipment, Unit = "台", Quantity = 4, HourlyCost = 150m, Notes = "功能/老化/气密性测试一体机" },
                new() { Code = "MFR-MEA-ISO", Name = "ISO质量体系", Type = ResourceType.Measure, Unit = "项", Quantity = 1, UnitPrice = 80000m, Notes = "ISO9001认证+年度监督审核" },
                new() { Code = "MFR-MEA-SAFE", Name = "安全生产措施", Type = ResourceType.Measure, Unit = "项", Quantity = 1, UnitPrice = 50000m, Notes = "安全培训/劳保用品/应急预案" },
            },
            ["AD-"] = new()
            {
                new() { Code = "AD-MAT-PRINT", Name = "印刷物料", Type = ResourceType.Material, Unit = "批", Quantity = 1, UnitPrice = 80000m, Notes = "海报/传单/展架/手提袋" },
                new() { Code = "AD-MAT-MEDIA", Name = "媒介资源位", Type = ResourceType.Material, Unit = "批", Quantity = 1, UnitPrice = 300000m, Notes = "开屏/信息流/搜索竞价" },
                new() { Code = "AD-MAT-PROP", Name = "拍摄道具", Type = ResourceType.Material, Unit = "批", Quantity = 1, UnitPrice = 20000m, Notes = "场景搭建/道具/服装" },
                new() { Code = "AD-EQU-CAM", Name = "摄影摄像器材", Type = ResourceType.Equipment, Unit = "套", Quantity = 2, HourlyCost = 500m, Notes = "ARRI Alexa/Red Komodo+镜头组" },
                new() { Code = "AD-EQU-EDIT", Name = "后期剪辑工作站", Type = ResourceType.Equipment, Unit = "套", Quantity = 3, HourlyCost = 80m, Notes = "Mac Studio+DaVinci Resolve" },
                new() { Code = "AD-MEA-RIGHT", Name = "肖像/音乐版权", Type = ResourceType.Measure, Unit = "项", Quantity = 1, UnitPrice = 50000m, Notes = "KOL肖像授权+背景音乐版权" },
                new() { Code = "AD-MEA-CONTRACT", Name = "KOL商务合同", Type = ResourceType.Measure, Unit = "份", Quantity = 30, UnitPrice = 2000m, Notes = "达人签约/法务审核/发票管理" },
            },
            ["MED-"] = new()
            {
                new() { Code = "MED-MAT-DRUG", Name = "试验药品", Type = ResourceType.Material, Unit = "盒", Quantity = 5000, UnitPrice = 200m, Notes = "研究药物+安慰剂+对照药" },
                new() { Code = "MED-MAT-REAG", Name = "试剂与耗材", Type = ResourceType.Material, Unit = "批", Quantity = 1, UnitPrice = 500000m, Notes = "生化试剂/血样管/离心管" },
                new() { Code = "MED-MAT-LAB", Name = "实验动物", Type = ResourceType.Material, Unit = "只", Quantity = 200, UnitPrice = 3000m, Notes = "SPF级SD大鼠/Beagle犬" },
                new() { Code = "MED-EQU-CTMS", Name = "临床试验管理系统", Type = ResourceType.Equipment, Unit = "套", Quantity = 1, HourlyCost = 50m, Notes = "EDC/CTMS/IWRS一体化平台" },
                new() { Code = "MED-EQU-LAB", Name = "实验室分析设备", Type = ResourceType.Equipment, Unit = "台", Quantity = 3, HourlyCost = 300m, Notes = "HPLC/LC-MS/MS/生化分析仪" },
                new() { Code = "MED-MEA-ETHICS", Name = "伦理审查", Type = ResourceType.Measure, Unit = "项", Quantity = 1, UnitPrice = 50000m, Notes = "伦理委员会审查批件" },
                new() { Code = "MED-MEA-GCP", Name = "GCP培训", Type = ResourceType.Measure, Unit = "批", Quantity = 1, UnitPrice = 30000m, Notes = "研究者和CRC GCP培训证书" },
                new() { Code = "MED-MEA-INSURE", Name = "受试者保险", Type = ResourceType.Measure, Unit = "份", Quantity = 1, UnitPrice = 100000m, Notes = "临床试验责任险" },
            },
            ["FIN-"] = new()
            {
                new() { Code = "FIN-MAT-SMS", Name = "短信/通知通道", Type = ResourceType.Material, Unit = "条", Quantity = 100000, UnitPrice = 0.035m, Notes = "验证码/交易通知/营销短信" },
                new() { Code = "FIN-MAT-3RD", Name = "第三方接口服务", Type = ResourceType.Material, Unit = "项", Quantity = 1, UnitPrice = 100000m, Notes = "征信/银企直连/实名认证API" },
                new() { Code = "FIN-MAT-CERT", Name = "数字证书", Type = ResourceType.Material, Unit = "张", Quantity = 2, UnitPrice = 30000m, Notes = "CFCA国密SSL证书+签章证书" },
                new() { Code = "FIN-EQU-HSM", Name = "加密机HSM", Type = ResourceType.Equipment, Unit = "台", Quantity = 2, HourlyCost = 30m, Notes = "国密GM/T 0030标准, 金融数据加密" },
                new() { Code = "FIN-EQU-PTEST", Name = "压力测试集群", Type = ResourceType.Equipment, Unit = "套", Quantity = 1, HourlyCost = 20m, Notes = "JMeter分布式压测+性能监控" },
                new() { Code = "FIN-MEA-DENGBAO", Name = "等保三级测评", Type = ResourceType.Measure, Unit = "项", Quantity = 1, UnitPrice = 200000m, Notes = "信息安全管理+技术+运维测评" },
                new() { Code = "FIN-MEA-DRILL", Name = "容灾演练", Type = ResourceType.Measure, Unit = "项", Quantity = 1, UnitPrice = 50000m, Notes = "同城双活/异地冷备切换演练" },
                new() { Code = "FIN-MEA-AUDIT", Name = "合规审计", Type = ResourceType.Measure, Unit = "项", Quantity = 1, UnitPrice = 150000m, Notes = "央行/银保监会合规要求审计" },
            },
            ["EDU-"] = new()
            {
                new() { Code = "EDU-MAT-BOOK", Name = "教材印刷", Type = ResourceType.Material, Unit = "册", Quantity = 2000, UnitPrice = 35m, Notes = "全彩学生用书+教师用书" },
                new() { Code = "EDU-MAT-TOOL", Name = "教具套装", Type = ResourceType.Material, Unit = "套", Quantity = 200, UnitPrice = 150m, Notes = "编程教具/机器人套件/实验箱" },
                new() { Code = "EDU-MAT-PLAT", Name = "学习平台账号", Type = ResourceType.Material, Unit = "个", Quantity = 1000, UnitPrice = 10m, Notes = "在线编程IDE+题库+社区" },
                new() { Code = "EDU-EQU-REC", Name = "录播设备", Type = ResourceType.Equipment, Unit = "套", Quantity = 2, HourlyCost = 100m, Notes = "4K摄像机+拾音+导播台" },
                new() { Code = "EDU-EQU-LIVE", Name = "直播教学平台", Type = ResourceType.Equipment, Unit = "套", Quantity = 1, HourlyCost = 30m, Notes = "保利威/ClassIn直播+互动工具" },
                new() { Code = "EDU-MEA-CR", Name = "课程版权登记", Type = ResourceType.Measure, Unit = "项", Quantity = 4, UnitPrice = 2000m, Notes = "L1-L4课程著作权登记" },
                new() { Code = "EDU-MEA-CERT", Name = "教师资格认证", Type = ResourceType.Measure, Unit = "项", Quantity = 1, UnitPrice = 10000m, Notes = "教师教学能力认证+AI编程资质" },
            },
            ["GOV-"] = new()
            {
                new() { Code = "GOV-MAT-SIGN", Name = "标志标牌", Type = ResourceType.Material, Unit = "批", Quantity = 1, UnitPrice = 80000m, Notes = "交通标志/指示牌/警示牌" },
                new() { Code = "GOV-MAT-CABLE", Name = "线缆", Type = ResourceType.Material, Unit = "m", Quantity = 50000, UnitPrice = 25m, Notes = "光缆/信号线/电源线综合" },
                new() { Code = "GOV-MAT-POLE", Name = "立杆/基础", Type = ResourceType.Material, Unit = "根", Quantity = 500, UnitPrice = 2000m, Notes = "6-8米八角杆+混凝土基础" },
                new() { Code = "GOV-EQU-SIGNAL", Name = "信号机", Type = ResourceType.Equipment, Unit = "台", Quantity = 120, HourlyCost = 5m, Notes = "多时段自适应控制信号机" },
                new() { Code = "GOV-EQU-CAM", Name = "高清摄像头", Type = ResourceType.Equipment, Unit = "台", Quantity = 500, HourlyCost = 2m, Notes = "900万像素AI抓拍一体机" },
                new() { Code = "GOV-EQU-SVR", Name = "数据中心服务器", Type = ResourceType.Equipment, Unit = "台", Quantity = 20, HourlyCost = 15m, Notes = "GPU服务器+存储阵列" },
                new() { Code = "GOV-MEA-SAFE", Name = "安全评估", Type = ResourceType.Measure, Unit = "项", Quantity = 1, UnitPrice = 100000m, Notes = "网络安全等级保护测评" },
                new() { Code = "GOV-MEA-TEST", Name = "第三方检测", Type = ResourceType.Measure, Unit = "项", Quantity = 1, UnitPrice = 80000m, Notes = "功能/性能/安全性第三方检测" },
            },
        };

        // 遍历所有项目，补充资源并自动分配
        foreach (var project in db.Projects.ToList())
        {
            // 根据项目代号前缀确定行业
            var prefix = extraResources.Keys.FirstOrDefault(k => project.Code.StartsWith(k));
            if (prefix == null) continue;

            var suppRes = extraResources[prefix];

            // 检查是否已添加过补充资源（避免重启重复导入）
            var existingCodes = db.Resources.Where(r => r.ProjectId == project.Id).Select(r => r.Code).ToHashSet();
            var toAdd = suppRes.Where(r => !existingCodes.Contains(r.Code)).ToList();
            if (toAdd.Count == 0) continue;

            foreach (var r in toAdd)
            {
                r.ProjectId = project.Id;
                r.CreatedAt = DateTime.UtcNow;
                db.Resources.Add(r);
            }
            db.SaveChanges();

            // 刷新资源列表
            var projResources = db.Resources.Where(r => r.ProjectId == project.Id).ToList();
            var resMap2 = projResources.ToDictionary(r => r.Code, r => r);

            // 获取所有任务
            var tasks = db.Tasks.Where(t => t.ProjectId == project.Id).OrderBy(t => t.SortOrder).ToList();
            if (tasks.Count == 0) continue;

            // 检查该任务是否已有人员分配，避免重复
            var existingAssignedTaskIds = db.ResourceAssignments
                .Where(ra => tasks.Select(t => t.Id).Contains(ra.TaskId))
                .Select(ra => ra.TaskId).ToHashSet();

            // 项目经理分配到所有任务（如果还未分配过）
            var humanRes = projResources.FirstOrDefault(r => r.Type == ResourceType.Labor);
            foreach (var task in tasks)
            {
                if (humanRes != null && !existingAssignedTaskIds.Contains(task.Id))
                {
                    db.ResourceAssignments.Add(new ResourceAssignment
                    {
                        TaskId = task.Id,
                        ResourceId = humanRes.Id,
                        Quantity = 1,
                        Notes = $"{task.Name} - 人员统筹"
                    });
                }
            }

            // 按行业精准分配材料/设备到具体任务
            var matPlan = GetIndustryMaterialPlan(project.Code, tasks);
            foreach (var (taskIdx, resCode, qty, notes) in matPlan)
            {
                if (taskIdx < tasks.Count && resMap2.TryGetValue(resCode, out var res))
                {
                    db.ResourceAssignments.Add(new ResourceAssignment
                    {
                        TaskId = tasks[taskIdx].Id,
                        ResourceId = res.Id,
                        Quantity = qty,
                        Notes = notes
                    });
                }
            }
            db.SaveChanges();
        }
    }

    private static List<(int taskIdx, string resCode, decimal qty, string notes)> GetIndustryMaterialPlan(string projectCode, List<TaskItem> tasks)
    {
        var plan = new List<(int taskIdx, string resCode, decimal qty, string notes)>();

        if (projectCode.StartsWith("IT-"))
        {
            // 智慧园区管理平台 — 开发/部署/测试任务消耗云资源和授权
            for (int i = 0; i < tasks.Count; i++)
            {
                var n = tasks[i].Name;
                if (n.Contains("Sprint"))
                {
                    plan.Add((i, "IT-MAT-CLOUD", 1m,     "Sprint 开发环境"));
                    plan.Add((i, "IT-MAT-LIC",   0.3m,   "Sprint 开发工具授权"));
                }
                else if (n.Contains("数据库") || n.Contains("部署") || n.Contains("试运行"))
                    plan.Add((i, "IT-MAT-CLOUD", 1m,     "环境资源"));
                else if (n.Contains("压测"))
                    plan.Add((i, "IT-MAT-CLOUD", 1.5m,   "压测环境"));
                else if (n.Contains("渗透"))
                    plan.Add((i, "IT-MAT-LIC",   0.5m,   "安全测试工具"));
                else if (n.Contains("联调"))
                    plan.Add((i, "IT-MAT-CLOUD", 0.5m,   "联调环境"));
                else if (n.Contains("上线"))
                {
                    plan.Add((i, "IT-MAT-CLOUD", 1m,     "生产环境"));
                    plan.Add((i, "IT-MAT-LIC",   0.2m,   "上线授权"));
                }
            }
        }
        else if (projectCode.StartsWith("MFR-"))
        {
            // 智能扫地机器人 — 原型/试产/量产消耗物料，设计阶段无材料消耗
            for (int i = 0; i < tasks.Count; i++)
            {
                var n = tasks[i].Name;
                if (n.Contains("原型"))
                {
                    plan.Add((i, "MFR-MAT-PCB",    10m,    "原型 PCB"));
                    plan.Add((i, "MFR-MAT-BATT",   5m,     "原型电池"));
                    plan.Add((i, "MFR-MAT-SENSOR", 5m,     "原型传感器"));
                }
                else if (n.Contains("小批量"))
                {
                    plan.Add((i, "MFR-MAT-PCB",    200m,   "试产 PCB"));
                    plan.Add((i, "MFR-MAT-BATT",   200m,   "试产电池"));
                    plan.Add((i, "MFR-MAT-SENSOR", 200m,   "试产传感器"));
                    plan.Add((i, "MFR-MAT-PACK",   200m,   "试产包装"));
                }
                else if (n.Contains("大批量"))
                {
                    plan.Add((i, "MFR-MAT-PCB",    5000m,  "量产 PCB"));
                    plan.Add((i, "MFR-MAT-BATT",   5000m,  "量产电池"));
                    plan.Add((i, "MFR-MAT-SENSOR", 5000m,  "量产传感器"));
                    plan.Add((i, "MFR-MAT-PACK",   50000m, "量产包装"));
                }
                else if (n.Contains("功能测试"))
                {
                    plan.Add((i, "MFR-MAT-PCB",    10m,    "测试样机 PCB"));
                    plan.Add((i, "MFR-MAT-BATT",   10m,    "测试样机电池"));
                    plan.Add((i, "MFR-MAT-SENSOR", 10m,    "测试样机传感器"));
                }
                else if (n.Contains("可靠性"))
                {
                    plan.Add((i, "MFR-MAT-PCB",    20m,    "可靠性测试样机 PCB"));
                    plan.Add((i, "MFR-MAT-BATT",   20m,    "可靠性测试样机电池"));
                }
            }
        }
        else if (projectCode.StartsWith("AD-"))
        {
            // 双11营销 — 创意/制作/投放阶段消耗物料和设备
            for (int i = 0; i < tasks.Count; i++)
            {
                var n = tasks[i].Name;
                if (n.Contains("创意概念"))
                {
                    plan.Add((i, "AD-MAT-PROP",    0.5m,   "创意参考道具"));
                }
                else if (n.Contains("视觉设计"))
                {
                    plan.Add((i, "AD-MAT-PRINT",   0.3m,   "设计打样"));
                    plan.Add((i, "AD-EQU-EDIT",    1m,     "设计工作站"));
                }
                else if (n.Contains("视频"))
                {
                    plan.Add((i, "AD-EQU-CAM",     2m,     "拍摄器材"));
                    plan.Add((i, "AD-EQU-EDIT",    2m,     "后期工作站"));
                    plan.Add((i, "AD-MAT-PROP",    1m,     "拍摄道具/场景"));
                }
                else if (n.Contains("社交") || n.Contains("内容创作"))
                {
                    plan.Add((i, "AD-EQU-CAM",     0.5m,   "内容拍摄"));
                    plan.Add((i, "AD-EQU-EDIT",    0.5m,   "内容剪辑"));
                }
                else if (n.Contains("预售") || n.Contains("预热"))
                {
                    plan.Add((i, "AD-MAT-MEDIA",   0.3m,   "预热期媒介"));
                }
                else if (n.Contains("正式期"))
                {
                    plan.Add((i, "AD-MAT-MEDIA",   1m,     "双11正式期媒介"));
                    plan.Add((i, "AD-MAT-PRINT",   0.5m,   "正式投放物料"));
                }
                else if (n.Contains("复盘"))
                {
                    plan.Add((i, "AD-EQU-EDIT",    0.5m,   "数据分析"));
                }
            }
        }
        else if (projectCode.StartsWith("MED-"))
        {
            // 新药研发 — 临床前/各期消耗药品和实验材料
            for (int i = 0; i < tasks.Count; i++)
            {
                var n = tasks[i].Name;
                if (n.Contains("药效") || n.Contains("毒理"))
                {
                    plan.Add((i, "MED-MAT-DRUG",   200m,   "临床前用药"));
                    plan.Add((i, "MED-MAT-LAB",    100m,   "实验动物"));
                    plan.Add((i, "MED-MAT-REAG",   0.3m,   "检测试剂"));
                }
                else if (n.Contains("IND"))
                {
                    plan.Add((i, "MED-MAT-REAG",   0.2m,   "申报分析试剂"));
                }
                else if (n.Contains("I 期"))
                {
                    plan.Add((i, "MED-MAT-DRUG",   500m,   "I期用药"));
                    plan.Add((i, "MED-MAT-REAG",   0.5m,   "I期检测试剂"));
                }
                else if (n.Contains("IIa"))
                {
                    plan.Add((i, "MED-MAT-DRUG",   1000m,  "IIa期用药"));
                    plan.Add((i, "MED-MAT-REAG",   0.8m,   "IIa期检测试剂"));
                }
                else if (n.Contains("IIb"))
                {
                    plan.Add((i, "MED-MAT-DRUG",   2000m,  "IIb期用药"));
                    plan.Add((i, "MED-MAT-REAG",   1m,     "IIb期检测试剂"));
                }
                else if (n.Contains("III 期"))
                {
                    plan.Add((i, "MED-MAT-DRUG",   5000m,  "III期用药"));
                    plan.Add((i, "MED-MAT-REAG",   3m,     "III期检测试剂"));
                }
                else if (n.Contains("NDA"))
                {
                    plan.Add((i, "MED-MAT-REAG",   0.5m,   "NDA补充检测"));
                }
                else if (n.Contains("生产验证"))
                {
                    plan.Add((i, "MED-MAT-DRUG",   500m,   "验证批用药"));
                }
            }
        }
        else if (projectCode.StartsWith("FIN-"))
        {
            // 金融科技 — 开发/安全/上线消耗加密和通信资源
            for (int i = 0; i < tasks.Count; i++)
            {
                var n = tasks[i].Name;
                if (n.Contains("监管合规"))
                {
                    plan.Add((i, "FIN-MEA-DENGBAO", 0.3m,  "合规预评估"));
                }
                else if (n.Contains("核心支付") || n.Contains("账户系统") || n.Contains("风控"))
                {
                    plan.Add((i, "FIN-EQU-HSM",    0.5m,   "加密机"));
                    plan.Add((i, "FIN-MAT-3RD",    0.3m,   "接口对接"));
                }
                else if (n.Contains("商户端"))
                {
                    plan.Add((i, "FIN-MAT-3RD",    0.2m,   "商户接口"));
                }
                else if (n.Contains("安全审计") || n.Contains("渗透"))
                {
                    plan.Add((i, "FIN-MEA-DENGBAO", 0.5m,  "安全测评"));
                }
                else if (n.Contains("沙盒"))
                {
                    plan.Add((i, "FIN-EQU-PTEST",  1m,     "沙盒测试环境"));
                }
                else if (n.Contains("灰度"))
                {
                    plan.Add((i, "FIN-EQU-PTEST",  0.5m,   "灰度压测"));
                }
                else if (n.Contains("全量上线"))
                {
                    plan.Add((i, "FIN-MAT-SMS",    50000m, "上线通知短信"));
                    plan.Add((i, "FIN-MAT-CERT",   1m,     "上线证书"));
                }
                else if (n.Contains("运维"))
                {
                    plan.Add((i, "FIN-MAT-SMS",    50000m, "运维通知短信"));
                    plan.Add((i, "FIN-MAT-CERT",   1m,     "运维证书"));
                }
            }
        }
        else if (projectCode.StartsWith("EDU-"))
        {
            // 教育培训 — 课程开发/内测/运营消耗教材和教具
            for (int i = 0; i < tasks.Count; i++)
            {
                var n = tasks[i].Name;
                if (n.Contains("课件") || n.Contains("练习平台") || n.Contains("AI 辅助"))
                {
                    plan.Add((i, "EDU-EQU-REC",    0.5m,   "录课设备"));
                    plan.Add((i, "EDU-MAT-PLAT",   500m,   "平台账号"));
                }
                else if (n.Contains("内测"))
                {
                    plan.Add((i, "EDU-MAT-BOOK",   200m,   "内测教材"));
                    plan.Add((i, "EDU-MAT-TOOL",   20m,    "内测教具"));
                }
                else if (n.Contains("迭代"))
                {
                    plan.Add((i, "EDU-MAT-PLAT",   200m,   "迭代测试账号"));
                }
                else if (n.Contains("正式发布"))
                {
                    plan.Add((i, "EDU-EQU-LIVE",   1m,     "直播发布"));
                }
                else if (n.Contains("开班"))
                {
                    plan.Add((i, "EDU-MAT-BOOK",   1000m,  "正式教材"));
                    plan.Add((i, "EDU-MAT-TOOL",   100m,   "正式教具"));
                    plan.Add((i, "EDU-MAT-PLAT",   500m,   "正式平台账号"));
                }
            }
        }
        else if (projectCode.StartsWith("GOV-"))
        {
            // 政府项目 — 采集/信号/监控消耗硬件和基础设施
            for (int i = 0; i < tasks.Count; i++)
            {
                var n = tasks[i].Name;
                if (n.Contains("数据采集"))
                {
                    plan.Add((i, "GOV-MAT-POLE",   200m,   "检测器立杆"));
                    plan.Add((i, "GOV-MAT-CABLE",  10000m, "检测器线缆"));
                }
                else if (n.Contains("信号控制"))
                {
                    plan.Add((i, "GOV-EQU-SIGNAL", 60m,    "信号机"));
                    plan.Add((i, "GOV-MAT-CABLE",  20000m, "信号线缆"));
                }
                else if (n.Contains("视频监控"))
                {
                    plan.Add((i, "GOV-EQU-CAM",    300m,   "摄像头"));
                    plan.Add((i, "GOV-MAT-POLE",   300m,   "监控立杆"));
                    plan.Add((i, "GOV-MAT-CABLE",  15000m, "监控线缆"));
                }
                else if (n.Contains("数据中心"))
                {
                    plan.Add((i, "GOV-EQU-SVR",    15m,    "数据中心服务器"));
                }
                else if (n.Contains("平台开发"))
                {
                    plan.Add((i, "GOV-EQU-SVR",    5m,     "开发服务器"));
                }
                else if (n.Contains("集成联调"))
                {
                    plan.Add((i, "GOV-MAT-CABLE",  5000m,  "调试线缆"));
                }
                else if (n.Contains("试运行"))
                {
                    plan.Add((i, "GOV-EQU-CAM",    200m,   "补充摄像头"));
                    plan.Add((i, "GOV-EQU-SIGNAL", 60m,    "补充信号机"));
                }
            }
        }

        return plan;
    }

    private static List<(int predIdx, int succIdx, int lag)> GetIndustryRelationPlan(string projectCode, List<TaskItem> tasks)
    {
        var plan = new List<(int predIdx, int succIdx, int lag)>();

        if (projectCode.StartsWith("IT-"))
        {
            // 智慧园区管理平台 — 需求/架构/Sprint/UAT/部署
            plan = new()
            {
                (0, 1, 0),   // 需求调研 → 产品原型
                (0, 3, 0),   // 需求调研 → 系统架构（并行）
                (1, 2, 0),   // 产品原型 → 需求评审
                (3, 4, 0),   // 系统架构 → 数据库设计
                (3, 5, 0),   // 系统架构 → Sprint1（并行启动）
                (3, 6, 0),   // 系统架构 → Sprint2
                (3, 7, 0),   // 系统架构 → Sprint3
                (3, 8, 0),   // 系统架构 → Sprint4
                (4, 5, 0),   // 数据库 → Sprint1
                (4, 6, 0),   // 数据库 → Sprint2
                (4, 7, 0),   // 数据库 → Sprint3
                (4, 8, 0),   // 数据库 → Sprint4
                (5, 9, 0),   // Sprint1 → 接口联调
                (6, 9, 0),   // Sprint2 → 接口联调
                (7, 9, 0),   // Sprint3 → 接口联调
                (8, 9, 0),   // Sprint4 → 接口联调
                (8, 10, 0),  // Sprint4 → 安全渗透
                (9, 11, 0),  // 接口联调 → 性能压测
                (9, 12, 0),  // 接口联调 → UAT
                (10, 12, 0), // 安全渗透 → UAT
                (11, 12, 0), // 性能压测 → UAT
                (12, 13, 0), // UAT → 生产部署
                (13, 14, 0), // 生产部署 → 试运行
                (14, 15, 0), // 试运行 → 正式上线
            };
        }
        else if (projectCode.StartsWith("MFR-"))
        {
            // 智能扫地机器人 — 设计/原型/测试/试产/量产
            plan = new()
            {
                (0, 1, 0),   // 市场调研 → 产品规格定义
                (1, 2, 0),   // 规格定义 → ID设计
                (1, 3, 0),   // 规格定义 → 结构设计（并行）
                (1, 4, 0),   // 规格定义 → 电子硬件设计（并行）
                (1, 5, 0),   // 规格定义 → 嵌入式软件（并行）
                (1, 6, 0),   // 规格定义 → App开发（并行）
                (2, 7, 0),   // ID设计 → 原型打样
                (3, 7, 0),   // 结构设计 → 原型打样
                (4, 7, 0),   // 电子硬件 → 原型打样
                (5, 7, 0),   // 嵌入式软件 → 原型打样
                (6, 7, 0),   // App开发 → 原型打样
                (7, 8, 0),   // 原型 → 功能测试
                (8, 9, 0),   // 功能测试 → 可靠性测试
                (9, 10, 0),  // 可靠性 → 模具开发
                (10, 11, 0), // 模具 → 小批量试产
                (11, 12, 0), // 小批量 → 产线调试
                (12, 13, 0), // 产线 → 大批量量产
                (13, 14, 0), // 大批量 → QC质检 (实际被覆盖为 SS+14d)
                (13, 15, 0), // 大批量 → 发布上市 (FS, 量产完成后发布)
            };
        }
        else if (projectCode.StartsWith("AD-"))
        {
            // 双11营销 — 按实际节奏：Brief/研究→策略→创意→制作→媒介→预热→爆发→复盘
            plan = new()
            {
                (0, 2, 0),   // AD-01 Brief → AD-03 策略
                (1, 2, 0),   // AD-02 竞品研究 → AD-03 策略
                (2, 3, 0),   // AD-03 策略 → AD-04 创意概念
                (3, 4, 0),   // AD-04 创意概念 → AD-05 视觉设计
                (3, 5, 0),   // AD-04 创意概念 → AD-06 视频制作
                (3, 6, 0),   // AD-04 创意概念 → AD-07 社交内容
                (3, 7, 0),   // AD-04 创意概念 → AD-08 KOL筛选
                (4, 8, 0),   // AD-05 视觉 → AD-09 媒介计划
                (5, 8, 0),   // AD-06 视频 → AD-09 媒介计划
                (6, 8, 0),   // AD-07 社交 → AD-09 媒介计划
                (7, 8, 0),   // AD-08 KOL → AD-09 媒介计划
                (8, 9, 32),  // AD-09 媒介计划 → AD-10 预热 (32d lag, 9月中→10月20)
                (8, 11, 13), // AD-09 媒介计划 → AD-12 数据监测 (13d lag, 9月中→10月1)
                (9, 10, 7),  // AD-10 预热 → AD-11 正式期投放 (7d lag, 11月3→11月11)
                (10, 12, 4), // AD-11 正式期 → AD-13 复盘 (4d lag, 11月13→11月18)
                (11, 12, 0), // AD-12 数据监测 → AD-13 复盘
            };
        }
        else if (projectCode.StartsWith("MED-"))
        {
            // 新药研发 — 药效/毒理→IND→各期→NDA→上市
            plan = new()
            {
                (0, 2, 0),   // 药效学 → IND申报
                (1, 2, 0),   // 毒理学 → IND申报
                (2, 3, 0),   // IND → I期临床
                (3, 4, 0),   // I期 → IIa期
                (4, 5, 0),   // IIa → IIb期
                (5, 6, 0),   // IIb → III期
                (6, 7, 0),   // III期 → NDA申报
                (7, 8, 0),   // NDA → 现场核查
                (7, 9, 0),   // NDA → 生产验证（并行）
                (8, 10, 0),  // 核查 → 上市定价
                (9, 10, 0),  // 生产验证 → 上市定价
                (10, 11, 0), // 上市 → IV期研究
            };
        }
        else if (projectCode.StartsWith("FIN-"))
        {
            // 金融科技 — 需求→各模块开发→合规/审计→沙盒/灰度→上线
            plan = new()
            {
                (0, 1, 0),   // 需求 → 支付引擎
                (0, 2, 0),   // 需求 → 账户系统（并行）
                (0, 3, 0),   // 需求 → 风控系统（并行）
                (0, 4, 0),   // 需求 → 商户端（并行）
                (1, 5, 0),   // 支付引擎 → 合规审查
                (2, 5, 0),   // 账户系统 → 合规审查
                (3, 5, 0),   // 风控系统 → 合规审查
                (4, 5, 0),   // 商户端 → 合规审查
                (1, 6, 0),   // 支付引擎 → 安全审计
                (2, 6, 0),   // 账户系统 → 安全审计
                (3, 6, 0),   // 风控系统 → 安全审计
                (4, 6, 0),   // 商户端 → 安全审计
                (6, 7, 0),   // 安全审计 → 渗透测试
                (5, 8, 0),   // 合规审查 → 沙盒环境
                (7, 8, 0),   // 渗透测试 → 沙盒环境
                (8, 9, 0),   // 沙盒 → 灰度发布
                (9, 10, 0),  // 灰度 → 全量上线
                (10, 11, 0), // 全量上线 → 运维监控
                (10, 12, 0), // 全量上线 → 持续迭代
            };
        }
        else if (projectCode.StartsWith("EDU-"))
        {
            // 教育培训 — 大纲→课件/AI/平台→内测→发布→开班
            plan = new()
            {
                (0, 1, 0),   // 大纲 → 课件开发
                (0, 2, 0),   // 大纲 → AI辅助系统（并行）
                (0, 3, 0),   // 大纲 → 练习平台（并行）
                (1, 5, 0),   // 课件 → 内测
                (2, 5, 0),   // AI系统 → 内测
                (3, 5, 0),   // 练习平台 → 内测
                (4, 5, 0),   // 教师认证 → 内测
                (5, 6, 0),   // 内测 → 迭代
                (6, 7, 0),   // 迭代 → 正式发布
                (7, 8, 0),   // 发布 → 市场推广
                (7, 9, 0),   // 发布 → 首批开班（并行）
                (9, 10, 0),  // 开班 → 教学评估
                (8, 11, 0),  // 推广 → 二期规划
                (10, 11, 0), // 评估 → 二期规划
            };
        }
        else if (projectCode.StartsWith("GOV-"))
        {
            // 政府项目 — 方案→各子系统→联调→试运行→验收
            plan = new()
            {
                (0, 1, 0),   // 方案 → 数据采集系统
                (0, 2, 0),   // 方案 → 信号控制系统（并行）
                (0, 3, 0),   // 方案 → 视频监控系统（并行）
                (0, 4, 0),   // 方案 → 数据中心（并行）
                (0, 5, 0),   // 方案 → 管理平台（并行）
                (1, 6, 0),   // 数据采集 → 联调
                (2, 6, 0),   // 信号控制 → 联调
                (3, 6, 0),   // 视频监控 → 联调
                (4, 6, 0),   // 数据中心 → 联调
                (5, 6, 0),   // 管理平台 → 联调
                (6, 7, 0),   // 联调 → 试运行
                (7, 8, 0),   // 试运行 → 验收交付
                (7, 9, 0),   // 试运行 → 人员培训（并行）
                (7, 10, 0),  // 试运行 → 运维体系（并行）
                (7, 11, 0),  // 试运行 → 文档归档（并行）
                (8, 12, 0),  // 验收 → 项目总结
                (9, 12, 0),  // 培训 → 项目总结
                (10, 12, 0), // 运维 → 项目总结
                (11, 12, 0), // 归档 → 项目总结
            };
        }

        return plan;
    }

    private static List<(Project project, List<TaskDef> tasks, List<Resource> resources, List<(int taskIdx, string resCode, decimal qty, string notes)>? assignments)> GetDemoProjects()
    {
        var baseYear = 2026;
        return new List<(Project, List<TaskDef>, List<Resource>, List<(int, string, decimal, string)>?)>
        {
            // ================================================================
            // 1. IT 软件研发 — 智慧园区管理平台 v2.0
            // ================================================================
            (new Project
            {
                Code = "IT-2026-001",
                Name = "智慧园区管理平台 v2.0",
                Description = "基于微服务架构的园区综合管理平台，涵盖门禁、能耗、物业、访客等模块",
                PlanStartDate = new DateTime(baseYear, 3, 7),
                PlanEndDate = new DateTime(baseYear, 9, 30),
                WorkingHoursPerDay = 8, WorkdaysPerWeek = 5,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            },
            new List<TaskDef>
            {
                new("PA-01", "需求调研与分析", 20, new DateTime(baseYear, 3, 7), "张敏", 0, 40000, false, "完成 12 个部门的需求访谈，输出需求规格说明书 PRD"),
                new("PA-02", "产品原型设计", 15, new DateTime(baseYear, 3, 22), "李思思", 0, 25000, false, "Axure 高保真原型，包含 8 个核心模块"),
                new("PA-03", "需求评审确认", 5, new DateTime(baseYear, 4, 7), "张敏", 100, 5000, true, "里程碑 — 需求基线冻结"),
                new("PA-04", "系统架构设计", 15, new DateTime(baseYear, 4, 7), "王磊", 0, 35000, false, "微服务拆分方案、技术选型、API 设计"),
                new("PA-05", "数据库设计与搭建", 10, new DateTime(baseYear, 4, 22), "王磊", 0, 15000, false, "MySQL 分库分表方案 + Redis 缓存设计"),
                new("PA-06", "Sprint 1 - 用户与权限模块", 25, new DateTime(baseYear, 4, 22), "赵岩", 0, 80000, false, "用户管理/角色权限/组织架构/登录注册"),
                new("PA-07", "Sprint 2 - 智能门禁模块", 25, new DateTime(baseYear, 5, 20), "陈浩", 0, 75000, false, "人脸识别/访客预约/远程开门/出入记录"),
                new("PA-08", "Sprint 3 - 能耗监测模块", 25, new DateTime(baseYear, 6, 16), "赵岩", 0, 70000, false, "水电气数据采集/异常预警/报表分析"),
                new("PA-09", "Sprint 4 - 物业管理模块", 25, new DateTime(baseYear, 7, 14), "陈浩", 0, 70000, false, "报修工单/巡检计划/设备台账/公告通知"),
                new("PA-10", "接口联调测试", 20, new DateTime(baseYear, 8, 10), "刘婷", 0, 30000, false, "前后端联调 + 第三方系统对接测试"),
                new("PA-11", "安全渗透测试", 15, new DateTime(baseYear, 8, 10), "刘婷", 0, 40000, false, "OWASP Top 10 检测、第三方安全审计"),
                new("PA-12", "性能压测与优化", 10, new DateTime(baseYear, 8, 25), "王磊", 0, 20000, false, "5000 并发压测，QPS 优化至 8000+"),
                new("PA-13", "UAT 用户验收测试", 15, new DateTime(baseYear, 9, 1), "张敏", 0, 15000, false, "各科室关键用户参与验收，签字确认"),
                new("PA-14", "生产环境部署", 5, new DateTime(baseYear, 9, 16), "王磊", 100, 10000, true, "里程碑 — 全量灰度发布，监控 48 小时"),
                new("PA-15", "系统试运行", 30, new DateTime(baseYear, 9, 16), "张敏", 0, 20000, false, "双轨运行期，问题收集与应急响应"),
                new("PA-16", "正式上线与验收", 3, new DateTime(baseYear, 10, 18), "张敏", 0, 5000, false, "旧系统关停，新系统全量切换，验收报告签署"),
            },
            new List<Resource>
            {
                new() { Code = "IT-PM", Name = "项目经理", Type = ResourceType.Labor, Unit = "人", Quantity = 1, HourlyCost = 120m, Notes = "PMP 认证" },
                new() { Code = "IT-PD", Name = "产品经理", Type = ResourceType.Labor, Unit = "人", Quantity = 1, HourlyCost = 110m, Notes = "5 年 B 端产品经验" },
                new() { Code = "IT-ARCH", Name = "架构师", Type = ResourceType.Labor, Unit = "人", Quantity = 1, HourlyCost = 150m, Notes = "微服务架构" },
                new() { Code = "IT-DEV", Name = "全栈开发工程师", Type = ResourceType.Labor, Unit = "人", Quantity = 6, HourlyCost = 90m, Notes = "Java/React" },
                new() { Code = "IT-TEST", Name = "测试工程师", Type = ResourceType.Labor, Unit = "人", Quantity = 2, HourlyCost = 70m, Notes = "自动化测试" },
                new() { Code = "IT-UI", Name = "UI/UX 设计师", Type = ResourceType.Labor, Unit = "人", Quantity = 1, HourlyCost = 80m, Notes = "Figma 设计系统" },
                new() { Code = "IT-DEVSRV", Name = "开发服务器", Type = ResourceType.Equipment, Unit = "台", Quantity = 5, HourlyCost = 5m, Notes = "云服务器 ECS" },
                new() { Code = "IT-TESTENV", Name = "测试环境", Type = ResourceType.Equipment, Unit = "套", Quantity = 2, HourlyCost = 3m, Notes = "K8s 测试集群" },
            },
            new List<(int taskIdx, string resCode, decimal qty, string notes)>
            {
                (0, "IT-PM", 1, "项目统筹"), (0, "IT-PD", 1, "需求调研"), (3, "IT-ARCH", 1, "架构设计"),
                (5, "IT-DEV", 4, "Sprint1 开发"), (5, "IT-UI", 1, "UI 支撑"), (9, "IT-TEST", 2, "联调测试"),
            }),
            // ================================================================
            // 2. 制造业 — 新一代智能扫地机器人
            // ================================================================
            (new Project
            {
                Code = "MFR-2026-001",
                Name = "新一代智能扫地机器人研发与量产",
                Description = "自研 LDS 激光导航 + 视觉融合避障的全链路产品研发，从概念到量产",
                PlanStartDate = new DateTime(baseYear, 1, 15),
                PlanEndDate = new DateTime(baseYear, 11, 25),
                WorkingHoursPerDay = 8, WorkdaysPerWeek = 5,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            },
            new List<TaskDef>
            {
                new("MR-01", "市场调研与竞品分析", 25, new DateTime(baseYear, 1, 15), "刘明", 100, 50000, false, "国内外 Top 10 竞品拆解分析报告"),
                new("MR-02", "产品规格定义", 1, new DateTime(baseYear, 2, 9), "刘明", 100, 10000, true, "里程碑 — PRD 签批, 目标售价 2499 元"),
                new("MR-03", "ID 工业设计", 30, new DateTime(baseYear, 2, 10), "周文", 0, 60000, false, "3D 建模+CMF+手板验证"),
                new("MR-04", "结构设计", 45, new DateTime(baseYear, 2, 10), "周文", 0, 80000, false, "模具 DFM 评审, 整机结构堆叠"),
                new("MR-05", "电子硬件设计", 40, new DateTime(baseYear, 2, 10), "吴波", 0, 100000, false, "主板原理图/PCB Layout/LDS 模组选型"),
                new("MR-06", "嵌入式软件开发", 60, new DateTime(baseYear, 2, 10), "陈涛", 0, 150000, false, "RTOS + SLAM 算法 + 避障策略"),
                new("MR-07", "App 端开发", 45, new DateTime(baseYear, 2, 10), "李想", 0, 90000, false, "iOS+Android 双端, 固件 OTA 升级"),
                new("MR-08", "原型机打样", 20, new DateTime(baseYear, 4, 11), "吴波", 0, 80000, false, "CNC 手板 10 台, 整机组装验证"),
                new("MR-09", "功能测试", 25, new DateTime(baseYear, 5, 1), "孙丽", 0, 40000, false, "清扫覆盖率/越障/断点续扫等 120 项用例"),
                new("MR-10", "可靠性测试", 30, new DateTime(baseYear, 5, 26), "孙丽", 0, 60000, false, "跌落/高低温/盐雾/寿命 3000h 连续运行"),
                new("MR-11", "模具开发", 45, new DateTime(baseYear, 6, 25), "周文", 0, 300000, false, "注塑模具 8 副, T1→T2→试模"),
                new("MR-12", "小批量试产", 15, new DateTime(baseYear, 8, 9), "赵刚", 0, 50000, false, "200 台试产, 直通率≥90%"),
                new("MR-13", "产线调试", 20, new DateTime(baseYear, 8, 24), "赵刚", 0, 80000, false, "SMT 贴片线/总装线/测试线联调"),
                new("MR-14", "大批量量产", 60, new DateTime(baseYear, 9, 13), "赵刚", 0, 500000, false, "首批 5 万台, 日产目标 1500 台"),
                new("MR-15", "QC 质检入库", 10, new DateTime(baseYear, 9, 27), "孙丽", 0, 20000, false, "AQL 抽检+全检, 入库放行"),
                new("MR-16", "产品发布上市", 1, new DateTime(baseYear, 11, 12), "刘明", 0, 100000, true, "里程碑 — 线上发布会+全渠道开售"),
            },
            new List<Resource>
            {
                new() { Code = "MFR-PM", Name = "产品经理", Type = ResourceType.Labor, Unit = "人", Quantity = 1, HourlyCost = 130m, Notes = "硬件产品经理" },
                new() { Code = "MFR-ID", Name = "工业设计师", Type = ResourceType.Labor, Unit = "人", Quantity = 2, HourlyCost = 100m, Notes = "Rhino/ProE" },
                new() { Code = "MFR-EE", Name = "电子工程师", Type = ResourceType.Labor, Unit = "人", Quantity = 3, HourlyCost = 110m, Notes = "Altium Designer" },
                new() { Code = "MFR-SW", Name = "嵌入式软件工程师", Type = ResourceType.Labor, Unit = "人", Quantity = 4, HourlyCost = 100m, Notes = "C/RTOS/SLAM" },
                new() { Code = "MFR-ME", Name = "结构工程师", Type = ResourceType.Labor, Unit = "人", Quantity = 3, HourlyCost = 100m, Notes = "Creo/DFM" },
                new() { Code = "MFR-QC", Name = "品质工程师", Type = ResourceType.Labor, Unit = "人", Quantity = 2, HourlyCost = 80m, Notes = "六西格玛" },
            },
            new List<(int taskIdx, string resCode, decimal qty, string notes)>
            {
                (0, "MFR-PM", 1, "市场调研统筹"), (3, "MFR-ME", 2, "结构设计"), (4, "MFR-EE", 2, "硬件设计"),
                (5, "MFR-SW", 3, "嵌入式开发"), (11, "MFR-QC", 1, "试产质量把控"),
            }),
            // ================================================================
            // 3. 广告创意与营销 — 双11全案整合营销
            // ================================================================
            (new Project
            {
                Code = "AD-2026-001",
                Name = "双11全案整合营销 Campaign",
                Description = "某消费电子品牌双11全案策划与执行，涵盖策略、创意、制作、投放、复盘全链路",
                PlanStartDate = new DateTime(baseYear, 7, 13),
                PlanEndDate = new DateTime(baseYear, 11, 30),
                WorkingHoursPerDay = 8, WorkdaysPerWeek = 5,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            },
            new List<TaskDef>
            {
                new("AD-01", "客户 Brief 解读", 5, new DateTime(baseYear, 7, 13), "杨丽", 100, 5000, false, "需求拆解、目标对齐、预算确认"),
                new("AD-02", "行业与竞品分析", 7, new DateTime(baseYear, 7, 13), "刘畅", 100, 8000, false, "竞品投放策略/创意方向/媒体分布"),
                new("AD-03", "核心策略制定", 10, new DateTime(baseYear, 7, 20), "杨丽", 100, 15000, true, "里程碑 — 策略提案过稿"),
                new("AD-04", "创意概念提案", 15, new DateTime(baseYear, 7, 27), "黄迪", 0, 20000, false, "3 个创意方向, 含 moodboard+storyboard"),
                new("AD-05", "视觉设计（主视觉+延展）", 25, new DateTime(baseYear, 8, 3), "黄迪", 0, 40000, false, "KV/商详页/社媒头图/Dou+素材/线下物料"),
                new("AD-06", "视频广告制作", 20, new DateTime(baseYear, 8, 10), "徐导", 0, 80000, false, "TVC 1 支 + 信息流短视频 8 支"),
                new("AD-07", "社交媒体内容创作", 20, new DateTime(baseYear, 8, 10), "林琳", 0, 25000, false, "小红书/抖音/微博内容排期 60 篇"),
                new("AD-08", "KOL 达人筛选签约", 15, new DateTime(baseYear, 8, 3), "刘畅", 0, 100000, false, "头部 2 人 + 腰部 8 人 + KOC 20 人"),
                new("AD-09", "媒介投放计划", 10, new DateTime(baseYear, 9, 1), "王敏", 0, 15000, false, "媒体组合排期/预算分配/出价策略"),
                new("AD-10", "预售期预热推广", 15, new DateTime(baseYear, 10, 20), "王敏", 0, 200000, false, "种草内容 + 预热短视频 + 直播间预热"),
                new("AD-11", "双11正式期投放", 3, new DateTime(baseYear, 11, 11), "杨丽", 0, 500000, true, "里程碑 — 全渠道流量收割, 目标 GMV 8000 万"),
                new("AD-12", "数据监测与优化", 30, new DateTime(baseYear, 10, 1), "王敏", 0, 20000, false, "每日数据看板、预算调优、素材替换"),
                new("AD-13", "收官复盘报告", 10, new DateTime(baseYear, 11, 18), "杨丽", 0, 10000, false, "全案效果总结、ROI 分析、经验沉淀"),
            },
            new List<Resource>
            {
                new() { Code = "AD-AM", Name = "客户经理", Type = ResourceType.Labor, Unit = "人", Quantity = 1, HourlyCost = 110m, Notes = "5 年广告行业经验" },
                new() { Code = "AD-PL", Name = "策略总监", Type = ResourceType.Labor, Unit = "人", Quantity = 1, HourlyCost = 150m, Notes = "品牌策略" },
                new() { Code = "AD-CD", Name = "创意总监", Type = ResourceType.Labor, Unit = "人", Quantity = 1, HourlyCost = 150m, Notes = "Copy Base" },
                new() { Code = "AD-DES", Name = "视觉设计师", Type = ResourceType.Labor, Unit = "人", Quantity = 2, HourlyCost = 80m, Notes = "PS/AI/C4D" },
                new() { Code = "AD-CW", Name = "文案", Type = ResourceType.Labor, Unit = "人", Quantity = 2, HourlyCost = 70m, Notes = "社媒内容" },
                new() { Code = "AD-MED", Name = "媒介投放", Type = ResourceType.Labor, Unit = "人", Quantity = 1, HourlyCost = 90m, Notes = "巨量引擎/腾讯广告" },
            },
            null),
            // ================================================================
            // 4. 医疗健康 — 新型口服降糖药 III 期临床
            // ================================================================
            (new Project
            {
                Code = "MED-2026-001",
                Name = "新型口服降糖药 III 期临床研发",
                Description = "全新机制 SGLT2/DPP-4 双靶点抑制剂, 从临床前到 NDA 申报全流程",
                PlanStartDate = new DateTime(baseYear, 1, 15),
                PlanEndDate = new DateTime(baseYear, 9, 30),
                WorkingHoursPerDay = 8, WorkdaysPerWeek = 5,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            },
            new List<TaskDef>
            {
                new("MD-01", "临床前药效学研究", 90, new DateTime(baseYear, 1, 15), "陈博士", 0, 2000000, false, "动物模型药效验证, 剂量爬坡"),
                new("MD-02", "临床前毒理学研究", 90, new DateTime(baseYear, 1, 15), "陈博士", 0, 3000000, false, "急毒/慢毒/生殖毒/致癌性"),
                new("MD-03", "IND 申报材料准备", 45, new DateTime(baseYear, 4, 15), "赵敏", 0, 500000, false, "CTD 模块撰写, 药学/药理/毒理汇总"),
                new("MD-04", "IND 审评", 60, new DateTime(baseYear, 6, 1), "赵敏", 0, 100000, true, "里程碑 — CDE 默示许可, 获临床试验批件"),
                new("MD-05", "I 期临床试验", 120, new DateTime(baseYear, 8, 1), "李医生", 0, 5000000, false, "健康受试者 60 例, 安全性+药代动力学"),
                new("MD-06", "IIa 期概念验证", 180, new DateTime(baseYear, 12, 1), "李医生", 0, 8000000, false, "患者 120 例, 初步疗效探索"),
                new("MD-07", "IIb 期剂量探索", 240, new DateTime(baseYear, 6, 1), "李医生", 0, 12000000, false, "患者 300 例, 3 个剂量组+安慰剂"),
                new("MD-08", "III 期多中心试验", 365, new DateTime(baseYear, 2, 1), "李医生", 0, 50000000, true, "里程碑 — 全国 30 家中心, 2000 例患者"),
                new("MD-09", "数据管理与统计分析", 150, new DateTime(baseYear, 2, 1), "孙数据", 0, 3000000, false, "EDC 数据清理+锁库+SAP 统计分析"),
                new("MD-10", "NDA 申报材料准备", 90, new DateTime(baseYear, 5, 1), "赵敏", 0, 2000000, false, "临床总结报告+药学+药理毒理+说明书"),
                new("MD-11", "NDA 审评", 180, new DateTime(baseYear, 8, 1), "赵敏", 0, 200000, false, "CDE 技术审评+临床核查+生产现场核查"),
                new("MD-12", "生产验证批", 60, new DateTime(baseYear, 8, 1), "王生产", 0, 5000000, false, "3 批商业化规模验证批, 稳定性考察启动"),
            },
            new List<Resource>
            {
                new() { Code = "MED-PM", Name = "临床项目经理", Type = ResourceType.Labor, Unit = "人", Quantity = 1, HourlyCost = 180m, Notes = "5 年以上 CRO 管理经验" },
                new() { Code = "MED-CRA", Name = "临床监查员 CRA", Type = ResourceType.Labor, Unit = "人", Quantity = 4, HourlyCost = 100m, Notes = "3 年以上监查经验" },
                new() { Code = "MED-DM", Name = "数据管理员", Type = ResourceType.Labor, Unit = "人", Quantity = 2, HourlyCost = 90m, Notes = "SAS/EDC" },
                new() { Code = "MED-RA", Name = "注册专员", Type = ResourceType.Labor, Unit = "人", Quantity = 1, HourlyCost = 130m, Notes = "CTD 申报" },
                new() { Code = "MED-STAT", Name = "生物统计师", Type = ResourceType.Labor, Unit = "人", Quantity = 1, HourlyCost = 140m, Notes = "SAS/R" },
            },
            null),
            // ================================================================
            // 5. 金融科技 — 移动支付平台 3.0
            // ================================================================
            (new Project
            {
                Code = "FIN-2026-001",
                Name = "移动支付平台 3.0",
                Description = "新一代聚合支付平台，支持数字人民币、跨境支付、多商户管理",
                PlanStartDate = new DateTime(baseYear, 4, 11),
                PlanEndDate = new DateTime(baseYear, 11, 30),
                WorkingHoursPerDay = 8, WorkdaysPerWeek = 5,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            },
            new List<TaskDef>
            {
                new("FN-01", "监管政策合规评审", 30, new DateTime(baseYear, 4, 11), "胡律", 0, 60000, false, "央行 259 号文/《非银行支付机构条例》解读"),
                new("FN-02", "产品方案设计", 20, new DateTime(baseYear, 5, 1), "高翔", 0, 50000, false, "产品架构/交互流程/商户端/用户端"),
                new("FN-03", "安全架构设计", 25, new DateTime(baseYear, 5, 15), "邓工", 0, 60000, false, "国密 SM2/SM3/SM4 方案、密钥管理体系"),
                new("FN-04", "核心支付引擎开发", 60, new DateTime(baseYear, 6, 1), "邓工", 0, 200000, false, "清结算/对账/路由/风控规则引擎"),
                new("FN-05", "账户系统开发", 45, new DateTime(baseYear, 7, 1), "马涛", 0, 150000, false, "Ⅱ/Ⅲ 类户体系、实名认证、余额管理"),
                new("FN-06", "风控系统开发", 50, new DateTime(baseYear, 7, 15), "邓工", 0, 180000, false, "实时反欺诈/交易监控/黑白名单/额度管理"),
                new("FN-07", "商户端开发", 40, new DateTime(baseYear, 8, 15), "马涛", 0, 130000, false, "商户入驻/交易查询/结算管理/对账单"),
                new("FN-08", "全链路安全审计", 30, new DateTime(baseYear, 9, 15), "邓工", 0, 80000, false, "代码审计/渗透测试/数据加密审计"),
                new("FN-09", "渗透测试", 20, new DateTime(baseYear, 10, 1), "邓工", 0, 50000, false, "第三方安全团队黑盒+白盒测试"),
                new("FN-10", "监管沙盒测试", 45, new DateTime(baseYear, 10, 1), "胡律", 0, 100000, false, "央行金融科技创新监管工具入盒测试"),
                new("FN-11", "灰度发布", 15, new DateTime(baseYear, 11, 1), "高翔", 0, 30000, false, "5% 用户灰度, 观察 7 天无异常"),
                new("FN-12", "全量上线", 3, new DateTime(baseYear, 11, 16), "高翔", 100, 10000, true, "里程碑 — 全量开放, 目标日交易笔数 100 万"),
                new("FN-13", "运维保障", 60, new DateTime(baseYear, 11, 16), "邓工", 0, 60000, false, "7×24 值班, 限流/熔断/降级预案"),
            },
            new List<Resource>
            {
                new() { Code = "FIN-PM", Name = "产品总监", Type = ResourceType.Labor, Unit = "人", Quantity = 1, HourlyCost = 150m, Notes = "金融产品经验" },
                new() { Code = "FIN-ARCH", Name = "安全架构师", Type = ResourceType.Labor, Unit = "人", Quantity = 1, HourlyCost = 160m, Notes = "CISSP/CISP" },
                new() { Code = "FIN-DEV", Name = "后端开发工程师", Type = ResourceType.Labor, Unit = "人", Quantity = 8, HourlyCost = 100m, Notes = "Java/Go/分布式" },
                new() { Code = "FIN-COMP", Name = "合规经理", Type = ResourceType.Labor, Unit = "人", Quantity = 1, HourlyCost = 140m, Notes = "持牌机构合规经验" },
                new() { Code = "FIN-TEST", Name = "测试工程师", Type = ResourceType.Labor, Unit = "人", Quantity = 3, HourlyCost = 80m, Notes = "接口/安全测试" },
            },
            null),
            // ================================================================
            // 6. 教育培训 — 少儿编程 AI 课程体系
            // ================================================================
            (new Project
            {
                Code = "EDU-2026-001",
                Name = "少儿编程 AI 课程体系开发",
                Description = "面向 7-15 岁儿童的 AI 编程课程体系，涵盖课程内容、互动平台、师资培训",
                PlanStartDate = new DateTime(baseYear, 2, 13),
                PlanEndDate = new DateTime(baseYear, 9, 30),
                WorkingHoursPerDay = 8, WorkdaysPerWeek = 5,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            },
            new List<TaskDef>
            {
                new("ED-01", "用户需求分析", 15, new DateTime(baseYear, 2, 13), "赵老师", 100, 20000, false, "家长调研/竞品课程体验/行业报告"),
                new("ED-02", "课程大纲设计", 20, new DateTime(baseYear, 2, 16), "钱教授", 0, 30000, false, "L1-L4 四大阶段, 共 200 课时"),
                new("ED-03", "L1-L2 教学内容编写", 30, new DateTime(baseYear, 3, 8), "钱教授", 0, 50000, false, "Scratch+Python 入门, 60 课时教案+PPT"),
                new("ED-04", "L3-L4 教学内容编写", 30, new DateTime(baseYear, 4, 8), "钱教授", 0, 50000, false, "Python+AI 基础, 60 课时教案+PPT"),
                new("ED-05", "互动课件开发", 45, new DateTime(baseYear, 3, 8), "孙工", 0, 80000, false, "H5 互动课件 120 个, 含动画/答题/游戏化"),
                new("ED-06", "编程练习平台建设", 50, new DateTime(baseYear, 3, 20), "孙工", 0, 120000, false, "在线 IDE/自动判题/作品展示社区"),
                new("ED-07", "AI 辅助教学系统开发", 40, new DateTime(baseYear, 5, 1), "孙工", 0, 100000, false, "AI 答疑/智能批改/学情分析"),
                new("ED-08", "教师培训材料编写", 20, new DateTime(baseYear, 6, 1), "赵老师", 0, 20000, false, "讲师手册/课堂指引/常见问题"),
                new("ED-09", "种子用户内测", 30, new DateTime(baseYear, 6, 15), "赵老师", 0, 10000, false, "50 名种子学员免费体验, 收集反馈"),
                new("ED-10", "课程迭代优化", 20, new DateTime(baseYear, 7, 15), "钱教授", 0, 20000, false, "根据内测反馈优化内容和平台"),
                new("ED-11", "正式发布", 5, new DateTime(baseYear, 8, 5), "赵老师", 100, 5000, true, "里程碑 — 全渠道上架, 开放报名"),
                new("ED-12", "首批开班运营", 60, new DateTime(baseYear, 8, 10), "赵老师", 0, 30000, false, "10 个班/每班 12 人, 教学质量和满意度跟踪"),
            },
            new List<Resource>
            {
                new() { Code = "EDU-PM", Name = "课程产品经理", Type = ResourceType.Labor, Unit = "人", Quantity = 1, HourlyCost = 100m, Notes = "教育产品经验" },
                new() { Code = "EDU-SME", Name = "学科专家", Type = ResourceType.Labor, Unit = "人", Quantity = 2, HourlyCost = 120m, Notes = "计算机教育背景" },
                new() { Code = "EDU-DEV", Name = "前端开发工程师", Type = ResourceType.Labor, Unit = "人", Quantity = 2, HourlyCost = 90m, Notes = "Vue/React/H5" },
                new() { Code = "EDU-BE", Name = "后端开发工程师", Type = ResourceType.Labor, Unit = "人", Quantity = 2, HourlyCost = 100m, Notes = "Python/Go" },
                new() { Code = "EDU-UI", Name = "课件设计师", Type = ResourceType.Labor, Unit = "人", Quantity = 1, HourlyCost = 70m, Notes = "教育游戏化设计" },
            },
            null),
            // ================================================================
            // 7. 政府与公共事业 — 数字城市智能交通管理
            // ================================================================
            (new Project
            {
                Code = "GOV-2026-001",
                Name = "数字城市智能交通管理系统",
                Description = "城市级智慧交通一期工程, 含交通数据采集/信号控制/视频监控/数据中心",
                PlanStartDate = new DateTime(baseYear, 3, 8),
                PlanEndDate = new DateTime(baseYear, 12, 31),
                WorkingHoursPerDay = 8, WorkdaysPerWeek = 5,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            },
            new List<TaskDef>
            {
                new("GV-01", "项目立项审批", 45, new DateTime(baseYear, 3, 8), "张局", 100, 50000, false, "立项报告编制/专家评审/发改委批复"),
                new("GV-02", "可研报告编制评审", 30, new DateTime(baseYear, 4, 15), "张局", 100, 80000, false, "可行性研究报告/财政局资金评审"),
                new("GV-03", "初步设计方案", 40, new DateTime(baseYear, 5, 15), "李工", 0, 120000, false, "整体方案设计/系统架构/技术路线"),
                new("GV-04", "公开招标采购", 60, new DateTime(baseYear, 6, 20), "王采购", 0, 50000, true, "里程碑 — 招标公告→开标→评标→中标公示"),
                new("GV-05", "交通数据采集系统", 90, new DateTime(baseYear, 8, 20), "李工", 0, 800000, false, "地磁/雷达/卡口/微波车辆检测器安装调试"),
                new("GV-06", "智能信号控制系统", 120, new DateTime(baseYear, 9, 1), "李工", 0, 1500000, false, "120 个路口信号机升级, 自适应控制算法"),
                new("GV-07", "视频监控系统", 90, new DateTime(baseYear, 9, 1), "李工", 0, 1200000, false, "500 路高清摄像头/车牌识别/违章抓拍"),
                new("GV-08", "数据中心建设", 60, new DateTime(baseYear, 10, 15), "陈工", 0, 2000000, false, "机房装修/服务器/Hadoop 大数据平台"),
                new("GV-09", "综合管理平台开发", 150, new DateTime(baseYear, 10, 1), "陈工", 0, 1500000, false, "交通态势/信号管控/指挥调度/运维管理"),
                new("GV-10", "系统集成联调", 45, new DateTime(baseYear, 11, 1), "李工", 0, 200000, false, "各子系统贯通, 数据中台对接, 大屏展示"),
                new("GV-11", "试运行", 60, new DateTime(baseYear, 11, 1), "张局", 0, 100000, false, "3 个示范区先试先行, 指标达标率≥95%"),
                new("GV-12", "竣工验收", 30, new DateTime(baseYear, 12, 1), "张局", 0, 50000, true, "里程碑 — 专家组验收/财政决算/移交运维"),
                new("GV-13", "运维交接", 30, new DateTime(baseYear, 12, 1), "陈工", 0, 80000, false, "运维手册/培训/备件移交/三年维保启动"),
            },
            new List<Resource>
            {
                new() { Code = "GOV-PM", Name = "项目办主任", Type = ResourceType.Labor, Unit = "人", Quantity = 1, HourlyCost = 150m, Notes = "政府项目管理" },
                new() { Code = "GOV-ENG", Name = "系统工程师", Type = ResourceType.Labor, Unit = "人", Quantity = 3, HourlyCost = 100m, Notes = "智能交通系统" },
                new() { Code = "GOV-DEV", Name = "软件开发工程师", Type = ResourceType.Labor, Unit = "人", Quantity = 5, HourlyCost = 90m, Notes = "大数据/可视化" },
                new() { Code = "GOV-FI", Name = "采购专员", Type = ResourceType.Labor, Unit = "人", Quantity = 1, HourlyCost = 80m, Notes = "政府采购法规" },
                new() { Code = "GOV-QA", Name = "质量监督员", Type = ResourceType.Labor, Unit = "人", Quantity = 2, HourlyCost = 80m, Notes = "工程监理" },
            },
            null),
        };
    }

    // ================================================================
    // 任务定义数据结构
    // ================================================================
    private record TaskDef
    {
        public string Code { get; }
        public string Name { get; }
        public int Duration { get; }
        public DateTime Start { get; }
        public string? Responsible { get; }
        public int Completion { get; }
        public decimal Budget { get; }
        public bool IsMilestone { get; }
        public string? Notes { get; }

        public TaskDef(string code, string name, int duration, DateTime start,
                       string? responsible, int completion, decimal budget,
                       bool isMilestone, string? notes)
        {
            Code = code;
            Name = name;
            Duration = duration;
            Start = start;
            Responsible = responsible;
            Completion = completion;
            Budget = budget;
            IsMilestone = isMilestone;
            Notes = notes;
        }
    }
}
