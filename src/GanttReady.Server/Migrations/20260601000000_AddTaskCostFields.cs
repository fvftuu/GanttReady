using Microsoft.EntityFrameworkCore.Migrations;

namespace GanttReady.Server.Migrations;

/// <inheritdoc />
public partial class AddTaskCostFields : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // SQLite 不支持 ALTER TABLE ADD COLUMN 的 DECIMAL 类型，使用 TEXT 存储
        migrationBuilder.Sql(
            "ALTER TABLE Tasks ADD COLUMN BudgetCost TEXT NOT NULL DEFAULT '0';");

        migrationBuilder.Sql(
            "ALTER TABLE Tasks ADD COLUMN ActualCost TEXT;");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // SQLite 不支持 DROP COLUMN，重建表
        // 此处省略重建逻辑（生产环境不建议降级）
        // 如需回滚，手动执行：
        // ALTER TABLE Tasks RENAME TO Tasks_old;
        // CREATE TABLE Tasks AS SELECT ..., 排除 BudgetCost, ActualCost FROM Tasks_old;
        // DROP TABLE Tasks_old;
    }
}
