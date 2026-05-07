using Microsoft.AspNetCore.Mvc;
using NetPlan.Server.Services;

namespace NetPlan.Server.Controllers;

/// <summary>
/// MS Project XML 导入 API
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProjectXmlController : ControllerBase
{
    private readonly ProjectXmlImportService _importService;
    private readonly IProjectService _projectService;
    private readonly ILogger<ProjectXmlController> _logger;

    public ProjectXmlController(
        ProjectXmlImportService importService,
        IProjectService projectService,
        ILogger<ProjectXmlController> logger)
    {
        _importService = importService;
        _projectService = projectService;
        _logger = logger;
    }

    /// <summary>
    /// 从 MS Project XML 文件导入任务和关系
    /// </summary>
    [HttpPost("import/{projectId}")]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<IActionResult> Import(int projectId, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "请选择文件" });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".xml")
            return BadRequest(new { error = "仅支持 .xml 格式（MS Project XML 导出）" });

        try
        {
            using var stream = file.OpenReadStream();
            var result = await _importService.ImportAsync(stream, projectId);

            // 导入后重算调度
            if (result.Success && result.TasksImported > 0)
            {
                await _projectService.CalculateScheduleAsync(projectId);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Project XML 导入失败");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
