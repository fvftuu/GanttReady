using Microsoft.AspNetCore.Mvc;
using GanttReady.Server.Services;

namespace GanttReady.Server.Controllers;

/// <summary>
/// Excel模板导入导出API
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ExcelController : ControllerBase
{
    private readonly ExcelTemplateService _excelService;
    private readonly ILogger<ExcelController> _logger;

    public ExcelController(ExcelTemplateService excelService, ILogger<ExcelController> logger)
    {
        _excelService = excelService;
        _logger = logger;
    }

    /// <summary>
    /// 导出项目Excel模板
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <returns>Excel文件</returns>
    [HttpGet("template/{projectId}")]
    public async Task<IActionResult> ExportTemplate(int projectId)
    {
        try
        {
            var fileData = await _excelService.GenerateTemplateAsync(projectId);
            return File(fileData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                $"NetPlan_项目模板_{projectId}.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出模板失败");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 导出空白导入模板
    /// </summary>
    /// <returns>空白Excel模板</returns>
    [HttpGet("blank-template")]
    public async Task<IActionResult> ExportBlankTemplate()
    {
        try
        {
            // 创建一个只有说明的空白模板
            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var ws = workbook.Worksheets.Add("使用说明");
            
            ws.Cell(1, 1).Value = "NetPlan Excel导入空白模板";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 16;
            
            var instructions = new[]
            {
                "",
                "请在以下工作表中填入数据：",
                "",
                "【任务数据】- 必须包含以下列：",
                "  A: 任务代码 (唯一标识)",
                "  B: 任务名称",
                "  C: 排序号",
                "  D: 父任务代码 (可选)",
                "  E: 计划开始日期 (yyyy-MM-dd)",
                "  F: 计划结束日期 (yyyy-MM-dd)",
                "  G: 计划工期 (天数)",
                "  H: 前置任务 (逗号分隔多个)",
                "  I: 关系类型 (FS/SS/FF/SF)",
                "  J: 时差",
                "",
                "【资源数据】- 必须包含以下列：",
                "  A: 资源代码 (唯一标识)",
                "  B: 资源名称",
                "  C: 资源类型 (Labor/Material/Equipment)",
                "  D: 单位",
                "  E: 数量",
                "  F: 单价",
                "  G: 小时成本",
                "  H: 备注",
                "",
                "【资源分配】- 必须包含以下列：",
                "  A: 任务代码",
                "  B: 资源代码",
                "  C: 分配数量",
                "  D: 备注"
            };

            int row = 3;
            foreach (var line in instructions)
            {
                ws.Cell(row, 1).Value = line;
                row++;
            }

            ws.Column(1).Width = 50;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(), 
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                "NetPlan_导入模板.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出空白模板失败");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 从Excel导入项目数据
    /// </summary>
    /// <param name="projectId">项目ID</param>
    /// <param name="file">Excel文件</param>
    /// <returns>导入结果</returns>
    [HttpPost("import/{projectId}")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10MB
    public async Task<IActionResult> ImportFromExcel(int projectId, IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "请选择要导入的Excel文件" });
        }

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) &&
            !file.FileName.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "请上传Excel文件 (.xlsx 或 .xls)" });
        }

        try
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            ms.Position = 0;

            var result = await _excelService.ImportFromExcelAsync(projectId, ms.ToArray());

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导入Excel失败");
            return BadRequest(new { error = ex.Message });
        }
    }
}
