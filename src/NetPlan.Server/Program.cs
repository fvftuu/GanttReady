using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using NetPlan.Server.Data;
using NetPlan.Server.Services;
using NetPlan.Server.Models;
using Microsoft.AspNetCore.Hosting;
using ClosedXML.Excel;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// EF Core + SQLite
builder.Services.AddDbContext<NetPlanDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection") ??
        $"Data Source={Path.Combine(builder.Environment.ContentRootPath, "netplan.db")}"));

// App services
builder.Services.AddScoped<IScheduleEngine, ScheduleEngine>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IResourceService, ResourceService>();
builder.Services.AddScoped<IAnalysisService, AnalysisService>();
builder.Services.AddScoped<ExcelTemplateService>();
builder.Services.AddScoped<ProjectXmlImportService>();
builder.Services.AddHttpClient();

var app = builder.Build();

// Middleware pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();

// 文件下载中间件 - 必须在Blazor Hub之前
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? "";
    
    if (path.StartsWith("/download/", StringComparison.OrdinalIgnoreCase))
    {
        try
        {
            var services = context.RequestServices;
            var excelService = services.GetRequiredService<ExcelTemplateService>();
            var tempPath = Path.Combine(Path.GetTempPath(), "netplan_downloads");
            Directory.CreateDirectory(tempPath);
            
            if (path == "/download/blank-template")
            {
                var filePath = Path.Combine(tempPath, "blank_template.xlsx");
                await GenerateBlankTemplateToFile(filePath, excelService);
                
                context.Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                var fileName = "NetPlan_blank_template.xlsx";
                context.Response.Headers["Content-Disposition"] = $"attachment; filename=\"{fileName}\"; filename*=UTF-8''{Uri.EscapeDataString("NetPlan_导入模板.xlsx")}";
                await context.Response.SendFileAsync(filePath);
                return;
            }
            else if (path.StartsWith("/download/template/"))
            {
                var idStr = path.Substring("/download/template/".Length);
                if (int.TryParse(idStr, out var projectId))
                {
                    var filePath = Path.Combine(tempPath, $"template_{projectId}.xlsx");
                    await GenerateProjectTemplateToFile(filePath, projectId, excelService);
                    
                    context.Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    var fileName = $"NetPlan_template_{projectId}.xlsx";
                    context.Response.Headers["Content-Disposition"] = $"attachment; filename=\"{fileName}\"; filename*=UTF-8''{Uri.EscapeDataString($"NetPlan_项目模板_{projectId}.xlsx")}";
                    await context.Response.SendFileAsync(filePath);
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsJsonAsync(new { error = ex.Message });
            return;
        }
    }
    
    await next();
});

app.UseEndpoints(endpoints =>
{
    // API路由
    endpoints.MapControllers();
    
    // 导出下载API
    endpoints.MapGet("/api/export/{projectId}", async (int projectId, IProjectService projectService) =>
    {
        string filePath;
        try
        {
            filePath = await projectService.ExportTasksToExcelAsync(projectId);
        }
        catch (Exception ex)
        {
            return Results.BadRequest(ex.Message);
        }
        
        if (!System.IO.File.Exists(filePath))
            return Results.NotFound("导出文件不存在");
        
        var bytes = await System.IO.File.ReadAllBytesAsync(filePath);
        var fileName = Path.GetFileName(filePath);
        return Results.File(bytes, "text/csv; charset=utf-8", fileName);
    });

    // 导出Excel (.xlsx) API - 使用ClosedXML生成真实Excel文件
    endpoints.MapGet("/api/export-excel/{projectId}", async (int projectId, ExcelTemplateService excelService, IProjectService projectService) =>
    {
        try
        {
            var project = await projectService.GetProjectByIdAsync(projectId);
            if (project == null)
                return Results.NotFound("项目不存在");

            var fileData = await excelService.GenerateTemplateAsync(projectId);
            var fileName = $"NetPlan_{project.Name}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return Results.File(fileData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    });

    // 导出资源Excel API
    endpoints.MapGet("/api/export-resources/{projectId}", async (int projectId, IResourceService resourceService) =>
    {
        try
        {
            var fileData = await resourceService.ExportResourcesToExcelAsync(projectId);
            var fileName = $"NetPlan_资源数据_{projectId}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return Results.File(fileData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    });

    // 导出全部资源Excel API
    endpoints.MapGet("/api/export-resources-all", async (IResourceService resourceService) =>
    {
        try
        {
            var fileData = await resourceService.ExportResourcesToExcelAsync(null);
            var fileName = $"NetPlan_全部资源_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return Results.File(fileData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    });

    // 导入资源Excel API
    endpoints.MapPost("/api/import-resources/{projectId}", async (int projectId, HttpRequest request, IResourceService resourceService) =>
    {
        try
        {
            if (!request.HasFormContentType || request.Form.Files.Count == 0)
                return Results.BadRequest(new { error = "请选择要导入的Excel文件" });

            var file = request.Form.Files[0];
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            
            var count = await resourceService.ImportResourcesFromExcelAsync(projectId, ms.ToArray());
            return Results.Ok(new { success = true, count, message = $"成功导入 {count} 条资源数据" });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    });

    // Blazor Server Hub
    endpoints.MapBlazorHub();
    endpoints.MapFallbackToPage("/_Host");
});

// 文件下载处理方法
async Task GenerateBlankTemplateToFile(string filePath, ExcelTemplateService excelService)
{
    using var workbook = new ClosedXML.Excel.XLWorkbook();
    
    // ========== Sheet 1: 任务与资源数据 ==========
    var ws1 = workbook.Worksheets.Add("任务与资源数据");
    
    // 表头 (Row 1)
    var headers1 = new[] { "序号", "任务代码", "任务名称", "负责人", "计划开始", "计划完成", "工期(天)", "实际开始", "实际完成", "前置任务", "时差", "关系类型", "完成率(%)", "资源名称", "资源数量", "单位", "备注" };
    for (int i = 0; i < headers1.Length; i++)
    {
        var cell = ws1.Cell(1, i + 1);
        cell.Value = headers1[i];
        cell.Style.Font.Bold = true;
        cell.Style.Fill.BackgroundColor = XLColor.LightGray;
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
    }

    // 示例数据 (Row 2)
    ws1.Cell(2, 1).Value = 1;
    ws1.Cell(2, 2).Value = "A1";
    ws1.Cell(2, 3).Value = "项目立项与审批";
    ws1.Cell(2, 4).Value = "张三";
    ws1.Cell(2, 5).Value = "2026-01-01";
    ws1.Cell(2, 6).Value = "2026-01-30";
    ws1.Cell(2, 7).Value = 30;
    ws1.Cell(2, 8).Value = "";  // 实际开始
    ws1.Cell(2, 9).Value = "";  // 实际完成
    ws1.Cell(2, 10).Value = ""; // 前置任务
    ws1.Cell(2, 11).Value = 0;  // 时差
    ws1.Cell(2, 12).Value = "FS"; // 关系类型
    ws1.Cell(2, 13).Value = 0;  // 完成率
    ws1.Cell(2, 14).Value = "项目经理"; // 资源名称
    ws1.Cell(2, 15).Value = 1;  // 资源数量
    ws1.Cell(2, 16).Value = "人"; // 单位
    ws1.Cell(2, 17).Value = "项目管理"; // 备注

    // 第二行示例数据 (Row 3)
    ws1.Cell(3, 1).Value = 2;
    ws1.Cell(3, 2).Value = "A2";
    ws1.Cell(3, 3).Value = "施工图设计";
    ws1.Cell(3, 4).Value = "李四";
    ws1.Cell(3, 5).Value = "2026-01-16";
    ws1.Cell(3, 6).Value = "2026-05-15";
    ws1.Cell(3, 7).Value = 120;
    ws1.Cell(3, 10).Value = "A1";
    ws1.Cell(3, 11).Value = 0;
    ws1.Cell(3, 12).Value = "FS";
    ws1.Cell(3, 14).Value = "港航工程师";
    ws1.Cell(3, 15).Value = 2;
    ws1.Cell(3, 16).Value = "人";

    // 列宽
    ws1.Column(1).Width = 6;   // 序号
    ws1.Column(2).Width = 10;  // 任务代码
    ws1.Column(3).Width = 20;  // 任务名称
    ws1.Column(4).Width = 10;  // 负责人
    ws1.Column(5).Width = 14;  // 计划开始
    ws1.Column(6).Width = 14;  // 计划完成
    ws1.Column(7).Width = 10;  // 工期
    ws1.Column(8).Width = 14;  // 实际开始
    ws1.Column(9).Width = 14;  // 实际完成
    ws1.Column(10).Width = 12; // 前置任务
    ws1.Column(11).Width = 6;  // 时差
    ws1.Column(12).Width = 8;  // 关系类型
    ws1.Column(13).Width = 10; // 完成率
    ws1.Column(14).Width = 15; // 资源名称
    ws1.Column(15).Width = 10; // 资源数量
    ws1.Column(16).Width = 8;  // 单位
    ws1.Column(17).Width = 15; // 备注
    
    // ========== Sheet 2: 资源数据 ==========
    var ws2 = workbook.Worksheets.Add("资源数据");
    
    var headers2 = new[] { "资源代码", "资源名称", "资源类型", "单位", "数量", "单价", "小时成本", "备注" };
    for (int i = 0; i < headers2.Length; i++)
    {
        ws2.Cell(1, i + 1).Value = headers2[i];
        ws2.Cell(1, i + 1).Style.Font.Bold = true;
        ws2.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        ws2.Cell(1, i + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws2.Cell(1, i + 1).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
    }
    
    // 示例数据
    ws2.Cell(2, 1).Value = "R001";
    ws2.Cell(2, 2).Value = "项目经理";
    ws2.Cell(2, 3).Value = "Labor";
    ws2.Cell(2, 4).Value = "人";
    ws2.Cell(2, 5).Value = 1;
    ws2.Cell(2, 6).Value = 0;
    ws2.Cell(2, 7).Value = 150;
    ws2.Cell(2, 8).Value = "一级建造师";
    
    ws2.Cell(3, 1).Value = "R002";
    ws2.Cell(3, 2).Value = "港航工程师";
    ws2.Cell(3, 3).Value = "Labor";
    ws2.Cell(3, 4).Value = "人";
    ws2.Cell(3, 5).Value = 2;
    ws2.Cell(3, 6).Value = 0;
    ws2.Cell(3, 7).Value = 100;
    ws2.Cell(3, 8).Value = "中级职称";
    
    ws2.Column(1).Width = 10;
    ws2.Column(2).Width = 15;
    ws2.Column(3).Width = 12;
    ws2.Column(4).Width = 8;
    ws2.Column(5).Width = 8;
    ws2.Column(6).Width = 10;
    ws2.Column(7).Width = 12;
    ws2.Column(8).Width = 15;
    
    workbook.SaveAs(filePath);
}

async Task GenerateProjectTemplateToFile(string filePath, int projectId, ExcelTemplateService excelService)
{
    var fileData = await excelService.GenerateTemplateAsync(projectId);
    await System.IO.File.WriteAllBytesAsync(filePath, fileData);
}

// Ensure DB created + seed demo data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NetPlanDbContext>();

    // 检测是否从旧 EnsureCreated 数据库过渡（表存在但无迁移历史）
    bool isExistingDb = db.Database.EnsureCreated();

    if (!isExistingDb)
    {
        // 已有数据库文件，检查是否缺迁移历史表
        var conn = db.Database.GetDbConnection();
        try
        {
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='__EFMigrationsHistory'";
            var historyExists = (long)cmd.ExecuteScalar()! > 0;
            if (!historyExists)
            {
                // 旧 EnsureCreated 数据库 → 创建迁移历史表，标记初始迁移已应用
                conn.Close(); conn.Open();
                cmd.CommandText = @"CREATE TABLE IF NOT EXISTS ""__EFMigrationsHistory"" (""MigrationId"" TEXT NOT NULL CONSTRAINT ""PK___EFMigrationsHistory"" PRIMARY KEY, ""ProductVersion"" TEXT NOT NULL)";
                cmd.ExecuteNonQuery();
                cmd.CommandText = @"INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"") VALUES ('20260506000000_InitialCreate', '8.0.0')";
                cmd.ExecuteNonQuery();
            }
        }
        finally { if (conn.State == System.Data.ConnectionState.Open) conn.Close(); }
    }

    SeedDemoData(db);
}

app.Run();

// ─── Demo Data ──────────────────────────────────────────
static void SeedDemoData(NetPlanDbContext db)
{
    // 只管理示例数据，不要删除用户创建的项目
    const string DEMO_PROJECT_CODE = "HP-2026-001";
    
    // 检查示例项目是否已存在
    if (db.Projects.Any(p => p.Code == DEMO_PROJECT_CODE))
    {
        return; // 示例项目已存在，跳过
    }

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

    // 创建任务关系（完整的项目网络图）
    var relations = new List<TaskRelation>
    {
        // A阶段内部关系
        new() { PredecessorTaskId = tasks[0].Id, SuccessorTaskId = tasks[1].Id, Type = RelationType.FS, Lag = 0 },
        new() { PredecessorTaskId = tasks[0].Id, SuccessorTaskId = tasks[2].Id, Type = RelationType.FS, Lag = 0 },
        new() { PredecessorTaskId = tasks[1].Id, SuccessorTaskId = tasks[3].Id, Type = RelationType.FS, Lag = 0 },
        new() { PredecessorTaskId = tasks[2].Id, SuccessorTaskId = tasks[4].Id, Type = RelationType.FS, Lag = 0 },
        new() { PredecessorTaskId = tasks[3].Id, SuccessorTaskId = tasks[4].Id, Type = RelationType.FS, Lag = 0 },

        // A → B 阶段转换
        new() { PredecessorTaskId = tasks[4].Id, SuccessorTaskId = tasks[5].Id, Type = RelationType.FS, Lag = 0 },

        // B阶段内部关系
        new() { PredecessorTaskId = tasks[5].Id, SuccessorTaskId = tasks[6].Id, Type = RelationType.FS, Lag = 15 },
        new() { PredecessorTaskId = tasks[6].Id, SuccessorTaskId = tasks[7].Id, Type = RelationType.FS, Lag = 0 },
        new() { PredecessorTaskId = tasks[7].Id, SuccessorTaskId = tasks[8].Id, Type = RelationType.FS, Lag = 30 },
        new() { PredecessorTaskId = tasks[8].Id, SuccessorTaskId = tasks[9].Id, Type = RelationType.FS, Lag = 30 },

        // B → C 阶段转换
        new() { PredecessorTaskId = tasks[9].Id, SuccessorTaskId = tasks[10].Id, Type = RelationType.FS, Lag = 0 },

        // C阶段内部关系（主体结构层层推进）
        new() { PredecessorTaskId = tasks[10].Id, SuccessorTaskId = tasks[11].Id, Type = RelationType.FS, Lag = 0 },
        new() { PredecessorTaskId = tasks[11].Id, SuccessorTaskId = tasks[12].Id, Type = RelationType.FS, Lag = 0 },
        new() { PredecessorTaskId = tasks[12].Id, SuccessorTaskId = tasks[13].Id, Type = RelationType.FS, Lag = 0 },
        new() { PredecessorTaskId = tasks[13].Id, SuccessorTaskId = tasks[14].Id, Type = RelationType.FS, Lag = 0 },
        new() { PredecessorTaskId = tasks[14].Id, SuccessorTaskId = tasks[15].Id, Type = RelationType.FS, Lag = 0 },
        new() { PredecessorTaskId = tasks[15].Id, SuccessorTaskId = tasks[16].Id, Type = RelationType.FS, Lag = 30 },
        new() { PredecessorTaskId = tasks[15].Id, SuccessorTaskId = tasks[17].Id, Type = RelationType.FS, Lag = 0 },

        // D阶段与C阶段并行
        new() { PredecessorTaskId = tasks[15].Id, SuccessorTaskId = tasks[18].Id, Type = RelationType.FS, Lag = 0 },  // 电气在结构封顶后开始
        new() { PredecessorTaskId = tasks[15].Id, SuccessorTaskId = tasks[19].Id, Type = RelationType.FS, Lag = 0 },  // 给排水
        new() { PredecessorTaskId = tasks[15].Id, SuccessorTaskId = tasks[20].Id, Type = RelationType.FS, Lag = 20 }, // 通风空调
        new() { PredecessorTaskId = tasks[15].Id, SuccessorTaskId = tasks[21].Id, Type = RelationType.FS, Lag = 30 }, // 消防
        new() { PredecessorTaskId = tasks[15].Id, SuccessorTaskId = tasks[22].Id, Type = RelationType.FS, Lag = 60 }, // 弱电
        new() { PredecessorTaskId = tasks[15].Id, SuccessorTaskId = tasks[23].Id, Type = RelationType.FS, Lag = 180 }, // 电梯

        // E阶段（装修收尾）
        new() { PredecessorTaskId = tasks[22].Id, SuccessorTaskId = tasks[24].Id, Type = RelationType.FS, Lag = 30 }, // 室内装修在弱电后
        new() { PredecessorTaskId = tasks[23].Id, SuccessorTaskId = tasks[24].Id, Type = RelationType.FS, Lag = 0 },  // 电梯完成后开始装修

        // E阶段内部关系
        new() { PredecessorTaskId = tasks[17].Id, SuccessorTaskId = tasks[25].Id, Type = RelationType.FS, Lag = 60 }, // 景观在外墙后
        new() { PredecessorTaskId = tasks[17].Id, SuccessorTaskId = tasks[26].Id, Type = RelationType.FS, Lag = 60 }, // 室外管网
        new() { PredecessorTaskId = tasks[26].Id, SuccessorTaskId = tasks[27].Id, Type = RelationType.FS, Lag = 30 }, // 道路在管网后

        // 最终验收流程
        new() { PredecessorTaskId = tasks[24].Id, SuccessorTaskId = tasks[28].Id, Type = RelationType.FS, Lag = 0 },  // 装修完成后验收准备
        new() { PredecessorTaskId = tasks[27].Id, SuccessorTaskId = tasks[28].Id, Type = RelationType.FS, Lag = 0 },  // 道路完成后验收准备
        new() { PredecessorTaskId = tasks[28].Id, SuccessorTaskId = tasks[29].Id, Type = RelationType.FS, Lag = 0 },  // 验收准备完成后正式验收
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
