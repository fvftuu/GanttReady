using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using NetPlan.Server.Data;
using NetPlan.Server.Services;
using NetPlan.Server.Models;
using NetPlan.Server;
using Microsoft.AspNetCore.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor(o =>
{
    o.DetailedErrors = builder.Environment.IsDevelopment();
});

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
builder.Services.AddScoped<WordExportService>();
builder.Services.AddScoped<ProjectXmlImportService>();
builder.Services.Configure<AiOptions>(builder.Configuration.GetSection(AiOptions.Section));
builder.Services.AddScoped<IAiService, AiService>();
builder.Services.AddSingleton<ToastService>();
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
                var fileData = await excelService.GenerateBlankTemplateAsync();
                var filePath = Path.Combine(tempPath, "blank_template.xlsx");
                await System.IO.File.WriteAllBytesAsync(filePath, fileData);
                
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
                    var fileData = await excelService.GenerateTemplateAsync(projectId);
                    var filePath = Path.Combine(tempPath, $"template_{projectId}.xlsx");
                    await System.IO.File.WriteAllBytesAsync(filePath, fileData);
                    
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

    // 导出Word分析报告 API（POST，表单方式提交 aiReport）
    endpoints.MapPost("/api/export-analysis-word/{projectId}", async (int projectId, WordExportService wordService, IProjectService projectService, HttpContext context) =>
    {
        try
        {
            var project = await projectService.GetProjectByIdAsync(projectId);
            if (project == null) return Results.NotFound("项目不存在");

            var aiReport = context.Request.Form["aiReport"].FirstOrDefault();
            var fileData = await wordService.ExportAnalysisReportAsync(projectId, aiReport);
            if (fileData == null || fileData.Length == 0)
                return Results.BadRequest(new { error = "生成的文件为空" });
            var fileName = $"NetPlan_{project.Name}_分析报告_{DateTime.Now:yyyyMMdd}.docx";
            return Results.File(fileData, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName);
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

    // 导出分析报告Excel API
    endpoints.MapGet("/api/export-analysis/{projectId}", async (int projectId, IAnalysisService analysisService) =>
    {
        try
        {
            var fileData = await analysisService.ExportAnalysisReportAsync(projectId);
            var fileName = $"NetPlan_分析报告_{projectId}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
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

        // 应用待执行的迁移（新迁移会被自动检测并执行）
        db.Database.Migrate();
    }

    DataSeeder.SeedDemoData(db);
    DemoProjectSeeder.Seed(db);
}

app.Run();
