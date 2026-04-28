using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using NetPlan.Server.Data;
using NetPlan.Server.Services;
using NetPlan.Server.Models;

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

var app = builder.Build();

// Middleware pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// Ensure DB created + seed demo data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NetPlanDbContext>();
    db.Database.EnsureCreated();
    SeedDemoData(db);
}

app.Run();

// ─── Demo Data ──────────────────────────────────────────
static void SeedDemoData(NetPlanDbContext db)
{
    if (db.Projects.Any()) return;   // already seeded

    var project = new Project
    {
        Code = "HP-2026-001",
        Name = "大连港航配套工程",
        Description = "码头疏浚、围堰、道路堆场及配套工程",
        PlanStartDate = new DateTime(2026, 5, 1),
        PlanEndDate   = new DateTime(2026, 12, 31),
        WorkingHoursPerDay = 8,
        WorkdaysPerWeek  = 5,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
    db.Projects.Add(project);
    db.SaveChanges();

    var startDate = new DateTime(2026, 5, 1);
    var tasks = new List<TaskItem>
    {
        new() { ProjectId = project.Id, Code = "A1", Name = "施工准备",     SortOrder = 1, PlanStartDate = startDate,                      PlanEndDate = startDate.AddDays(9),   PlanDuration = 10, CreatedAt = DateTime.UtcNow },
        new() { ProjectId = project.Id, Code = "A2", Name = "定位放线",     SortOrder = 2, PlanStartDate = startDate.AddDays(10),          PlanEndDate = startDate.AddDays(14),  PlanDuration = 5,  CreatedAt = DateTime.UtcNow },
        new() { ProjectId = project.Id, Code = "A3", Name = "基槽开挖",     SortOrder = 3, PlanStartDate = startDate.AddDays(15),          PlanEndDate = startDate.AddDays(44),  PlanDuration = 30, CreatedAt = DateTime.UtcNow },
        new() { ProjectId = project.Id, Code = "A4", Name = "沉箱预制",     SortOrder = 4, PlanStartDate = startDate.AddDays(15),          PlanEndDate = startDate.AddDays(74),  PlanDuration = 60, CreatedAt = DateTime.UtcNow },
        new() { ProjectId = project.Id, Code = "A5", Name = "沉箱出运安装", SortOrder = 5, PlanStartDate = startDate.AddDays(75),         PlanEndDate = startDate.AddDays(94),  PlanDuration = 20, CreatedAt = DateTime.UtcNow },
        new() { ProjectId = project.Id, Code = "A6", Name = "上部结构施工", SortOrder = 6, PlanStartDate = startDate.AddDays(95),         PlanEndDate = startDate.AddDays(139), PlanDuration = 45, CreatedAt = DateTime.UtcNow },
        new() { ProjectId = project.Id, Code = "A7", Name = "后方回填",     SortOrder = 7, PlanStartDate = startDate.AddDays(140),         PlanEndDate = startDate.AddDays(159), PlanDuration = 20, CreatedAt = DateTime.UtcNow },
        new() { ProjectId = project.Id, Code = "A8", Name = "竣工验收",     SortOrder = 8, PlanStartDate = startDate.AddDays(160),         PlanEndDate = startDate.AddDays(160), PlanDuration = 1,  CreatedAt = DateTime.UtcNow },
    };
    db.Tasks.AddRange(tasks);
    db.SaveChanges();

    // Create relations: 1→2(FS), 2→3(FS), 2→4(FS), 3→5(FS), 4→5(FS), 5→6(FS), 6→7(FS), 7→8(FS)
    var relations = new List<TaskRelation>
    {
        new() { PredecessorTaskId = tasks[0].Id, SuccessorTaskId = tasks[1].Id, Type = RelationType.FS, Lag = 0 },
        new() { PredecessorTaskId = tasks[1].Id, SuccessorTaskId = tasks[2].Id, Type = RelationType.FS, Lag = 0 },
        new() { PredecessorTaskId = tasks[1].Id, SuccessorTaskId = tasks[3].Id, Type = RelationType.FS, Lag = 0 },
        new() { PredecessorTaskId = tasks[2].Id, SuccessorTaskId = tasks[4].Id, Type = RelationType.FS, Lag = 0 },
        new() { PredecessorTaskId = tasks[3].Id, SuccessorTaskId = tasks[4].Id, Type = RelationType.FS, Lag = 0 },
        new() { PredecessorTaskId = tasks[4].Id, SuccessorTaskId = tasks[5].Id, Type = RelationType.FS, Lag = 0 },
        new() { PredecessorTaskId = tasks[5].Id, SuccessorTaskId = tasks[6].Id, Type = RelationType.FS, Lag = 0 },
        new() { PredecessorTaskId = tasks[6].Id, SuccessorTaskId = tasks[7].Id, Type = RelationType.FS, Lag = 0 },
    };
    db.TaskRelations.AddRange(relations);

    // Seed resources
    var resources = new List<Resource>
    {
        new() { ProjectId = project.Id, Name = "项目经理",   Type = ResourceType.Labor,    Quantity = 1,  HourlyCost = 150m,  Notes = "一级建造师",    Unit = "人",   UnitPrice = 0,    CreatedAt = DateTime.UtcNow },
        new() { ProjectId = project.Id, Name = "港航工程师", Type = ResourceType.Labor,    Quantity = 2,  HourlyCost = 100m,  Notes = "中级职称",      Unit = "人",   UnitPrice = 0,    CreatedAt = DateTime.UtcNow },
        new() { ProjectId = project.Id, Name = "挖泥船",     Type = ResourceType.Equipment, Quantity = 1,  HourlyCost = 2000m, Notes = "舱容≥4500m³",  Unit = "艘",   UnitPrice = 0,    CreatedAt = DateTime.UtcNow },
        new() { ProjectId = project.Id, Name = "起重船",     Type = ResourceType.Equipment, Quantity = 1,  HourlyCost = 3000m, Notes = "起吊≥500t",    Unit = "艘",   UnitPrice = 0,    CreatedAt = DateTime.UtcNow },
        new() { ProjectId = project.Id, Name = "混凝土",     Type = ResourceType.Material,  Quantity = 5000, HourlyCost = null, Notes = "C40，单位:m³", Unit = "m³", UnitPrice = 400m, CreatedAt = DateTime.UtcNow },
    };
    db.Resources.AddRange(resources);
    db.SaveChanges();
}
