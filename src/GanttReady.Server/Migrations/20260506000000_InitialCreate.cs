using Microsoft.EntityFrameworkCore.Migrations;

namespace GanttReady.Server.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ========== Projects ==========
        migrationBuilder.CreateTable(
            name: "Projects",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                PlanStartDate = table.Column<string>(type: "TEXT", nullable: false),
                PlanEndDate = table.Column<string>(type: "TEXT", nullable: false),
                ActualStartDate = table.Column<string>(type: "TEXT", nullable: true),
                ActualEndDate = table.Column<string>(type: "TEXT", nullable: true),
                WorkingHoursPerDay = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 8),
                WorkdaysPerWeek = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 5),
                CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Projects", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Projects_Code",
            table: "Projects",
            column: "Code",
            unique: true);

        // ========== Tasks ==========
        migrationBuilder.CreateTable(
            name: "Tasks",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                ProjectId = table.Column<int>(type: "INTEGER", nullable: false),
                Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                ParentTaskId = table.Column<int>(type: "INTEGER", nullable: true),
                OutlineLevel = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                PlanStartDate = table.Column<string>(type: "TEXT", nullable: false),
                PlanEndDate = table.Column<string>(type: "TEXT", nullable: false),
                PlanDuration = table.Column<int>(type: "INTEGER", nullable: false),
                ActualStartDate = table.Column<string>(type: "TEXT", nullable: true),
                ActualEndDate = table.Column<string>(type: "TEXT", nullable: true),
                ActualDuration = table.Column<int>(type: "INTEGER", nullable: true),
                ResponsiblePerson = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                CompletionPercentage = table.Column<int>(type: "INTEGER", nullable: false),
                IsMilestone = table.Column<bool>(type: "INTEGER", nullable: false),
                IsManualSchedule = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                EarlyStart = table.Column<int>(type: "INTEGER", nullable: true),
                EarlyFinish = table.Column<int>(type: "INTEGER", nullable: true),
                LateStart = table.Column<int>(type: "INTEGER", nullable: true),
                LateFinish = table.Column<int>(type: "INTEGER", nullable: true),
                TotalFloat = table.Column<int>(type: "INTEGER", nullable: true),
                FreeFloat = table.Column<int>(type: "INTEGER", nullable: true),
                IsCritical = table.Column<bool>(type: "INTEGER", nullable: false),
                ExtraData = table.Column<string>(type: "TEXT", nullable: true),
                CreatedAt = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Tasks", x => x.Id);
                table.ForeignKey(
                    name: "FK_Tasks_Projects_ProjectId",
                    column: x => x.ProjectId,
                    principalTable: "Projects",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Tasks_Tasks_ParentTaskId",
                    column: x => x.ParentTaskId,
                    principalTable: "Tasks",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Tasks_ProjectId",
            table: "Tasks",
            column: "ProjectId");

        migrationBuilder.CreateIndex(
            name: "IX_Tasks_ParentTaskId",
            table: "Tasks",
            column: "ParentTaskId");

        migrationBuilder.CreateIndex(
            name: "IX_Tasks_ProjectId_SortOrder",
            table: "Tasks",
            columns: new[] { "ProjectId", "SortOrder" });

        // ========== TaskRelations ==========
        migrationBuilder.CreateTable(
            name: "TaskRelations",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                PredecessorTaskId = table.Column<int>(type: "INTEGER", nullable: false),
                SuccessorTaskId = table.Column<int>(type: "INTEGER", nullable: false),
                Type = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                Lag = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TaskRelations", x => x.Id);
                table.ForeignKey(
                    name: "FK_TaskRelations_Tasks_PredecessorTaskId",
                    column: x => x.PredecessorTaskId,
                    principalTable: "Tasks",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_TaskRelations_Tasks_SuccessorTaskId",
                    column: x => x.SuccessorTaskId,
                    principalTable: "Tasks",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_TaskRelations_PredecessorTaskId",
            table: "TaskRelations",
            column: "PredecessorTaskId");

        migrationBuilder.CreateIndex(
            name: "IX_TaskRelations_SuccessorTaskId",
            table: "TaskRelations",
            column: "SuccessorTaskId");

        // ========== Resources ==========
        migrationBuilder.CreateTable(
            name: "Resources",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                ProjectId = table.Column<int>(type: "INTEGER", nullable: true),
                Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Type = table.Column<int>(type: "INTEGER", nullable: false),
                Unit = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                Quantity = table.Column<decimal>(type: "TEXT", nullable: false),
                UnitPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                HourlyCost = table.Column<decimal>(type: "TEXT", nullable: true),
                Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                ExtraData = table.Column<string>(type: "TEXT", nullable: true),
                CreatedAt = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Resources", x => x.Id);
                table.ForeignKey(
                    name: "FK_Resources_Projects_ProjectId",
                    column: x => x.ProjectId,
                    principalTable: "Projects",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Resources_ProjectId",
            table: "Resources",
            column: "ProjectId");

        // ========== ResourceAssignments ==========
        migrationBuilder.CreateTable(
            name: "ResourceAssignments",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                TaskId = table.Column<int>(type: "INTEGER", nullable: false),
                ResourceId = table.Column<int>(type: "INTEGER", nullable: false),
                Quantity = table.Column<decimal>(type: "TEXT", nullable: false),
                Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ResourceAssignments", x => x.Id);
                table.ForeignKey(
                    name: "FK_ResourceAssignments_Tasks_TaskId",
                    column: x => x.TaskId,
                    principalTable: "Tasks",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ResourceAssignments_Resources_ResourceId",
                    column: x => x.ResourceId,
                    principalTable: "Resources",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ResourceAssignments_TaskId",
            table: "ResourceAssignments",
            column: "TaskId");

        migrationBuilder.CreateIndex(
            name: "IX_ResourceAssignments_ResourceId",
            table: "ResourceAssignments",
            column: "ResourceId");

        // ========== ColumnDefinitions ==========
        migrationBuilder.CreateTable(
            name: "ColumnDefinitions",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                ProjectId = table.Column<int>(type: "INTEGER", nullable: false),
                ViewName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                FieldName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                DisplayName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                Width = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 100),
                SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                IsVisible = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                IsEditable = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ColumnDefinitions", x => x.Id);
                table.ForeignKey(
                    name: "FK_ColumnDefinitions_Projects_ProjectId",
                    column: x => x.ProjectId,
                    principalTable: "Projects",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ColumnDefinitions_ProjectId",
            table: "ColumnDefinitions",
            column: "ProjectId");

        migrationBuilder.CreateIndex(
            name: "IX_ColumnDefinitions_ProjectId_ViewName_SortOrder",
            table: "ColumnDefinitions",
            columns: new[] { "ProjectId", "ViewName", "SortOrder" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ColumnDefinitions");
        migrationBuilder.DropTable(name: "ResourceAssignments");
        migrationBuilder.DropTable(name: "Resources");
        migrationBuilder.DropTable(name: "TaskRelations");
        migrationBuilder.DropTable(name: "Tasks");
        migrationBuilder.DropTable(name: "Projects");
    }
}
