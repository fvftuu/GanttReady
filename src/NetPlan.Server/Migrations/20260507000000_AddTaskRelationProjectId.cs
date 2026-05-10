using Microsoft.EntityFrameworkCore.Migrations;

namespace NetPlan.Server.Migrations;

/// <inheritdoc />
public partial class AddTaskRelationProjectId : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "ProjectId",
            table: "TaskRelations",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.CreateIndex(
            name: "IX_TaskRelations_ProjectId",
            table: "TaskRelations",
            column: "ProjectId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_TaskRelations_ProjectId",
            table: "TaskRelations");

        migrationBuilder.DropColumn(
            name: "ProjectId",
            table: "TaskRelations");
    }
}
