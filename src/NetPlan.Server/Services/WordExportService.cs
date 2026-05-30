using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using NetPlan.Server.Models;
using System.Text.RegularExpressions;

namespace NetPlan.Server.Services;

public class WordExportService
{
    private readonly IProjectService _projectService;
    private readonly IAnalysisService _analysisService;
    private readonly IResourceService _resourceService;

    public WordExportService(IProjectService projectService, IAnalysisService analysisService, IResourceService resourceService)
    {
        _projectService = projectService;
        _analysisService = analysisService;
        _resourceService = resourceService;
    }

    public async Task<byte[]> ExportAnalysisReportAsync(int projectId, string? aiReport = null)
    {
        var project = await _projectService.GetProjectByIdAsync(projectId);
        if (project == null) throw new Exception("项目不存在");
        var tasks = await _projectService.GetTasksByProjectIdAsync(projectId);
        var analysis = _analysisService.AnalyzeProject(project, tasks);
        var evm = await _analysisService.GetEarnedValueAsync(projectId);
        var stage = await _analysisService.GetStageCompletionAsync(projectId);
        var sv = await _analysisService.GetScheduleVarianceAsync(projectId);
        var resources = await _resourceService.GetResourcesByProjectAsync(projectId);
        var assignments = await _resourceService.GetAssignmentsByProjectAsync(projectId);

        // 解析 AI 报告为 4 段文本（已剥离 HTML，保留文本）
        var ai = ParseAiSections(aiReport);
        var today = DateTime.Today;

        var stream = new MemoryStream();
        try
        {
            using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, autoSave: false))
            {
                var mainPart = doc.AddMainDocumentPart();
                mainPart.Document = new Document();
                var body = new Body();
                mainPart.Document.Append(body);

                // === 标题 ===
                AddH(body, "项目分析报告", 1);
                AddP(body, $"项目名称：{project.Name}　　报告日期：{today:yyyy-MM-dd}");
                AddP(body, $"项目代码：{project.Code}　　计划周期：{project.PlanStartDate:yyyy-MM-dd} → {project.PlanEndDate:yyyy-MM-dd}");
                AddP(body, $"总工期：{analysis.ProjectDurationDays} 天　　总任务数：{tasks.Count} 个");
                Blank(body);

                // 一、项目概况（融入 AI 项目总览 + 核心指标）
                AddH(body, "一、项目概况", 2);
                AddAiRichText(body, ai.GetValueOrDefault("overview"));
                var dc = analysis.LateStartTasks.Count;
                AddP(body, $"本项目共 {tasks.Count} 个任务，总计划工期 {analysis.ProjectDurationDays} 天。");
                AddP(body, $"按时完成 {analysis.OnTimeTasks.Count} 个，提前完成 {analysis.EarlyStartTasks.Count} 个，延后 {dc} 个，关键任务 {analysis.CriticalPathTasks.Count} 个。");
                if (dc > 0) AddP(body, $"注意：存在 {dc} 个延后任务，建议重点关注。");
                AddTable(body, new[] { "指标", "数值" }, new[] {
                    new[] { "总工期", $"{analysis.ProjectDurationDays} 天" }, new[] { "总任务数", $"{tasks.Count} 个" },
                    new[] { "按时完成", $"{analysis.OnTimeTasks.Count} 个" }, new[] { "提前完成", $"{analysis.EarlyStartTasks.Count} 个" },
                    new[] { "延后任务", $"{dc} 个" }, new[] { "关键任务", $"{analysis.CriticalPathTasks.Count} 个" } });
                Blank(body);

                // 二、任务完成状态明细（融入 AI 任务状态分类）
                AddH(body, "二、任务完成状态明细", 2);
                AddAiRichText(body, ai.GetValueOrDefault("tasks"));
                var ct = tasks.Count(t => t.CompletionPercentage >= 100);
                var ip = tasks.Count(t => t.CompletionPercentage > 0 && t.CompletionPercentage < 100);
                var ns = tasks.Count(t => t.CompletionPercentage <= 0);
                var ov = tasks.Count(t => t.CompletionPercentage <= 0 && today > t.PlanEndDate);
                AddP(body, $"已完成 {ct} 个，进行中 {ip} 个，未开始 {ns} 个（其中超期未开始 {ov} 个）。");
                if (ov > 0) AddP(body, $"有 {ov} 个任务已超期但尚未开始，需立即处理！");
                var tRows = tasks.OrderBy(t => t.PlanStartDate).Select(t => new[] {
                    t.Code ?? "", t.Name ?? "", $"{t.CompletionPercentage}%",
                    $"{t.PlanStartDate:MM/dd}-{t.PlanEndDate:MM/dd}", t.ActualEndDate?.ToString("MM/dd") ?? "—",
                    t.CompletionPercentage >= 100 ? "已完成" : t.CompletionPercentage > 0 ? "进行中" : today > t.PlanEndDate ? "超期未开始" : "未开始",
                    t.CompletionPercentage >= 100 && t.ActualEndDate.HasValue && t.ActualEndDate.Value.Date < t.PlanEndDate.Date ? "提前" : "" }).ToArray();
                if (tRows.Length > 0) { AddP(body, $"详细任务列表（共 {tRows.Length} 个）："); AddTable(body, new[] { "编码", "名称", "完成率", "计划周期", "实际完成", "状态", "备注" }, tRows); }
                Blank(body);

                // 三、挣值分析
                if (evm != null)
                {
                    AddH(body, "三、挣值分析 (EVM)", 2);
                    AddAiRichText(body, ai.GetValueOrDefault("evm"));
                    var sl = evm.SchedulePerformanceIndex >= 1.0 ? "正常" : evm.SchedulePerformanceIndex >= 0.9 ? "预警" : "滞后";
                    var cl = evm.CostPerformanceIndex >= 1.0 ? "正常" : evm.CostPerformanceIndex >= 0.9 ? "预警" : "超支";
                    var csl = evm.CostScheduleIndex >= 0.9 ? "可控" : "需关注";
                    AddP(body, $"PV：{evm.PlannedValue:N0}　EV：{evm.EarnedValue:N0}　AC：{evm.ActualCost:N0}");
                    AddP(body, $"SPI：{evm.SchedulePerformanceIndex:F2}（{sl}）　CPI：{evm.CostPerformanceIndex:F2}（{cl}）　CSI：{evm.CostScheduleIndex:F2}（{csl}）");
                    AddP(body, $"SV：{evm.ScheduleVariance:F0}　CV：{evm.CostVariance:F0}　BAC：{evm.BudgetAtCompletion:N0}　EAC：{evm.EstimateAtCompletion:N0}");
                    AddTable(body, new[] { "指标", "数值", "评价" }, new[] {
                        new[] { "PV", $"{evm.PlannedValue:N0}", "" }, new[] { "EV", $"{evm.EarnedValue:N0}", "" }, new[] { "AC", $"{evm.ActualCost:N0}", "" },
                        new[] { "SPI", $"{evm.SchedulePerformanceIndex:F2}", sl }, new[] { "CPI", $"{evm.CostPerformanceIndex:F2}", cl },
                        new[] { "CSI", $"{evm.CostScheduleIndex:F2}", csl }, new[] { "SV", $"{evm.ScheduleVariance:F0}", "" },
                        new[] { "CV", $"{evm.CostVariance:F0}", "" }, new[] { "BAC", $"{evm.BudgetAtCompletion:N0}", "" }, new[] { "EAC", $"{evm.EstimateAtCompletion:N0}", "" } });
                    Blank(body);
                }

                // 四、阶段完成率
                if (stage != null)
                {
                    AddH(body, "四、阶段完成率", 2);
                    AddP(body, $"检测方法：{stage.DetectionMethod}。完成进度：{stage.CompletedStages}/{stage.TotalStages} 阶段，整体完成率：{stage.OverallPct:F1}%");
                    var sr = stage.Stages.Take(10).Select(s => new[] { s.StageName, $"{s.PlannedCompletionPct}%", $"{s.ActualCompletionPct}%",
                        $"{s.CompletedTasks}/{s.TotalTasks}", s.Status == "completed" ? "已完成" : s.Status == "in_progress" ? "进行中" : s.Status == "delayed" ? "延迟" : "待办",
                        s.DelayDays.HasValue && s.DelayDays != 0 ? $"{s.DelayDays}天" : "—" }).ToArray();
                    if (sr.Length > 0) AddTable(body, new[] { "阶段", "计划", "实际", "完成", "状态", "偏差" }, sr);
                    Blank(body);
                }

                // 五、关键路径（融入 AI 详细进度对比）
                AddH(body, "五、关键路径", 2);
                AddAiRichText(body, ai.GetValueOrDefault("critical"));
                AddP(body, $"共 {analysis.CriticalPathTasks.Count} 个关键任务。");
                if (analysis.CriticalPathTasks.Count > 0)
                {
                    var cpr = analysis.CriticalPathTasks.Take(20).Select(t => new[] { t.Code ?? "", t.Name ?? "",
                        $"{t.PlanStartDate:MM/dd}→{t.PlanEndDate:MM/dd}", $"{t.PlanDuration}天",
                        t.TotalFloat.HasValue ? $"{t.TotalFloat}天" : "—", t.ResponsiblePerson ?? "—" }).ToArray();
                    AddTable(body, new[] { "编码", "名称", "计划周期", "工期", "时差", "负责人" }, cpr);
                }
                Blank(body);

                // 六、延后任务（融入 AI 问题与风险）
                if (analysis.LateStartTasks.Any())
                {
                    AddH(body, "六、延后任务", 2);
                    AddAiRichText(body, ai.GetValueOrDefault("risks"));
                    var td = analysis.LateStartTasks.Sum(t => t.ActualEndDate.HasValue ? (t.ActualEndDate.Value - t.PlanEndDate).Days : (today - t.PlanEndDate).Days);
                    AddP(body, $"共 {analysis.LateStartTasks.Count} 个延后任务，累计延后 {td} 天。");
                    var dr = analysis.LateStartTasks.OrderBy(t => t.PlanEndDate).Take(15).Select(t => {
                        var dd = t.ActualEndDate.HasValue ? (t.ActualEndDate.Value - t.PlanEndDate).Days : (today - t.PlanEndDate).Days;
                        return new[] { t.Code ?? "", t.Name ?? "", $"{t.PlanEndDate:MM/dd}", $"{dd}天", $"{t.CompletionPercentage}%", t.ResponsiblePerson ?? "—" };
                    }).ToArray();
                    AddTable(body, new[] { "编码", "名称", "计划完成", "延后", "完成率", "负责人" }, dr);
                    Blank(body);
                }

                // 七、里程碑
                if (analysis.Milestones.Any())
                {
                    AddH(body, "七、里程碑", 2);
                    var mr = analysis.Milestones.OrderBy(m => m.Date).Select(m => new[] { m.Name, m.Date.ToString("yyyy-MM-dd"),
                        m.Date <= today ? "已完成" : m.Date <= today.AddDays(14) ? "即将到期" : "未到" }).ToArray();
                    AddTable(body, new[] { "名称", "日期", "状态" }, mr);
                    Blank(body);
                }

                // 八、工期偏差
                if (sv != null)
                {
                    AddH(body, "八、工期偏差分析", 2);
                    AddP(body, $"提前 {sv.AheadCount} 个　按时 {sv.OnTimeCount} 个　延后 {sv.BehindCount} 个　进行中 {sv.InProgressCount} 个　未开始 {sv.NotStartedCount} 个");
                    AddP(body, $"延后累计 {sv.TotalDelayDays} 天　提前累计 {sv.TotalAheadDays} 天");
                    Blank(body);
                }

                // 九、资源
                AddH(body, "九、资源概况", 2);
                AddP(body, $"共 {resources.Count} 个资源，{assignments.Count} 条分配。");
                AddP(body, $"人工 {resources.Count(r => r.Type == ResourceType.Labor)} 个　材料 {resources.Count(r => r.Type == ResourceType.Material)} 个　设备 {resources.Count(r => r.Type == ResourceType.Equipment)} 个　金 {resources.Count(r => r.Type == ResourceType.Measure)} 个");
                var rr = resources.Take(20).Select(r => new[] { r.Name, r.Type.ToString(), r.Unit, r.Quantity.ToString(), r.UnitPrice.ToString("N2"), (r.Quantity * r.UnitPrice).ToString("N0") }).ToArray();
                if (rr.Length > 0) AddTable(body, new[] { "名称", "类型", "单位", "数量", "单价", "合计" }, rr);
                Blank(body);

                // 十、下一步行动计划（AI 第四段）
                var action = ai.GetValueOrDefault("action");
                if (!string.IsNullOrWhiteSpace(action))
                {
                    AddH(body, "十、下一步行动计划", 2);
                    AddAiRichText(body, action);
                }

                mainPart.Document.Save();
            }
            return stream.ToArray();
        }
        finally { stream.Dispose(); }
    }

    // ========== AI 解析：按 <h2> 拆分为 4 段，保留格式化文本 ==========

    private Dictionary<string, string> ParseAiSections(string? html)
    {
        var result = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(html)) return result;

        var sectionPattern = new Regex(@"<h2>(.*?)</h2>(.*?)(?=<h2>|$)", RegexOptions.Singleline);
        var matches = sectionPattern.Matches(html);

        foreach (Match m in matches)
        {
            var title = Regex.Replace(m.Groups[1].Value, @"<[^>]*>", "").Trim();
            var content = m.Groups[2].Value.Trim();
            // 将内部HTML转为带颜色标记的纯文本，保留 emoji
            var text = HtmlToRichText(content);

            if (title.Contains("总览") || title.Contains("概览")) result["overview"] = (result.TryGetValue("overview", out var o) ? o + "\n" : "") + text;
            else if (title.Contains("任务") || title.Contains("状态")) result["tasks"] = text;
            else if (title.Contains("进度对比") || title.Contains("详细进度")) result["evm"] = text;
            else if (title.Contains("关键")) result["critical"] = text;
            else if (title.Contains("问题") || title.Contains("风险")) result["risks"] = text;
            else if (title.Contains("行动") || title.Contains("计划") || title.Contains("建议")) result["action"] = text;
            else result["overview"] = (result.TryGetValue("overview", out var o2) ? o2 + "\n" : "") + text;
        }
        return result;
    }

    /// <summary>将 HTML 片段转为带 OpenXml 颜色标记的文本</summary>
    private static string HtmlToRichText(string html)
    {
        // 替换 <span style="color:xxx"> 为颜色标记
        html = Regex.Replace(html, @"<span[^>]*color:\s*(green|#3ecf8e)[^>]*>", "〖GREEN〗");
        html = Regex.Replace(html, @"<span[^>]*color:\s*(red|#ff5a5f|#ff4d4f)[^>]*>", "〖RED〗");
        html = Regex.Replace(html, @"<span[^>]*class=""text-warning""[^>]*>", "〖ORANGE〗");
        html = Regex.Replace(html, @"<span[^>]*class=""text-success""[^>]*>", "〖GREEN〗");
        html = Regex.Replace(html, @"<span[^>]*class=""text-danger""[^>]*>", "〖RED〗");
        html = Regex.Replace(html, @"</?span[^>]*>", ""); // 其他span
        html = Regex.Replace(html, @"<br\s*/?>", "\n");
        html = Regex.Replace(html, @"</?p[^>]*>", "\n");
        html = Regex.Replace(html, @"</?div[^>]*>", "\n");
        html = Regex.Replace(html, @"</?ul[^>]*>", "\n");
        html = Regex.Replace(html, @"</?li[^>]*>", "• ");
        html = Regex.Replace(html, @"</?h3[^>]*>", "\n〖H3〗");
        html = Regex.Replace(html, @"</?b>|</?i>", "");
        // 将表格转为纯文本（保留内容）
        html = Regex.Replace(html, @"<table[^>]*>", "\n", RegexOptions.Singleline);
        html = Regex.Replace(html, @"</table>", "\n", RegexOptions.Singleline);
        html = Regex.Replace(html, @"</tr>", "\n");
        html = Regex.Replace(html, @"<t[hd][^>]*>", "  "); // th/td 开始
        html = Regex.Replace(html, @"</t[hd]>", " │");
        html = Regex.Replace(html, @"<[^>]*>", ""); // 其他标签
        // 清理多余换行
        html = Regex.Replace(html, @"\n{3,}", "\n\n");
        return html.Trim();
    }

    /// <summary>将带颜色标记的文本渲染到 Word body</summary>
    private static void AddAiRichText(Body body, string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) continue;

            if (trimmed.StartsWith("〖H3〗"))
            {
                var content = trimmed.Replace("〖H3〗", "").Trim();
                body.Append(new Paragraph(
                    new ParagraphProperties(new SpacingBetweenLines { Before = "120", After = "60" }),
                    new Run(
                        new RunProperties(new Bold(), new FontSize { Val = "24" },
                            new RunFonts { Ascii = "微软雅黑", EastAsia = "微软雅黑" }, new Color { Val = "4f6ef7" }),
                        new Text(content))));
                continue;
            }

            // 解析颜色标记
            var segments = Regex.Split(trimmed, @"(〖GREEN〗|〖RED〗|〖ORANGE〗)");
            if (segments.Length <= 1 && !trimmed.StartsWith("•"))
            {
                AddP(body, trimmed);
                continue;
            }

            var para = new Paragraph();
            var pp = new ParagraphProperties(new SpacingBetweenLines { After = "40" });
            if (trimmed.StartsWith("•"))
                pp.Append(new Indentation { Left = "360", Hanging = "180" });
            para.Append(pp);
            foreach (var seg in segments)
            {
                if (string.IsNullOrEmpty(seg)) continue;
                if (seg == "〖GREEN〗") continue;
                if (seg == "〖RED〗") continue;
                if (seg == "〖ORANGE〗") continue;

                // 查找该段前面的颜色标记
                var idx = Array.IndexOf(segments, seg);
                var color = "333333";
                if (idx > 0)
                {
                    var prev = segments[idx - 1];
                    if (prev == "〖GREEN〗") color = "3ecf8e";
                    else if (prev == "〖RED〗") color = "ff5a5f";
                    else if (prev == "〖ORANGE〗") color = "f5a623";
                }

                var rp = new RunProperties(new FontSize { Val = "22" },
                    new RunFonts { Ascii = "微软雅黑", EastAsia = "微软雅黑" }, new Color { Val = color });
                para.Append(new Run(rp, new Text(seg) { Space = SpaceProcessingModeValues.Preserve }));
            }
            body.Append(para);
        }
    }

    // ========== 基础组件 ==========

    private static void AddH(Body body, string text, int level)
    {
        var pp = new ParagraphProperties(new SpacingBetweenLines { Before = level == 1 ? "300" : "240", After = "120" });
        var rp = new RunProperties(new RunFonts { Ascii = "微软雅黑", EastAsia = "微软雅黑", HighAnsi = "微软雅黑" },
            new Bold(), new FontSize { Val = level == 1 ? "36" : "28" }, new Color { Val = level == 1 ? "1a1a1a" : "4f6ef7" });
        body.Append(new Paragraph(pp, new Run(rp, new Text(text))));
    }

    private static void AddP(Body body, string text)
    {
        var rp = new RunProperties(new RunFonts { Ascii = "微软雅黑", EastAsia = "微软雅黑", HighAnsi = "微软雅黑" },
            new FontSize { Val = "22" }, new Color { Val = "333333" });
        body.Append(new Paragraph(new ParagraphProperties(new SpacingBetweenLines { After = "60", Line = "276", LineRule = LineSpacingRuleValues.Auto }),
            new Run(rp, new Text(text) { Space = SpaceProcessingModeValues.Preserve })));
    }

    private static void Blank(Body body) => body.Append(new Paragraph(new ParagraphProperties(new SpacingBetweenLines { Before = "60" })));

    private static void AddTable(Body body, string[] headers, string[][] rows)
    {
        var tbl = new Table();
        tbl.Append(new TableProperties(new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct },
            new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 4, Color = "d0d5e0" },
                new BottomBorder { Val = BorderValues.Single, Size = 4, Color = "d0d5e0" },
                new LeftBorder { Val = BorderValues.Single, Size = 4, Color = "d0d5e0" },
                new RightBorder { Val = BorderValues.Single, Size = 4, Color = "d0d5e0" },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4, Color = "d0d5e0" },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 4, Color = "d0d5e0" })));
        var hRow = new TableRow();
        foreach (var h in headers)
            hRow.Append(new TableCell(
                new TableCellProperties(new Shading { Val = ShadingPatternValues.Clear, Fill = "f0f2ff" },
                    new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }),
                new Paragraph(new Run(new RunProperties(new Bold(), new FontSize { Val = "20" }, new RunFonts { Ascii = "微软雅黑", EastAsia = "微软雅黑" }), new Text(h)))));
        tbl.Append(hRow);
        foreach (var row in rows)
        {
            var dRow = new TableRow();
            foreach (var c in row)
                dRow.Append(new TableCell(
                    new TableCellProperties(new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }),
                    new Paragraph(new Run(new RunProperties(new FontSize { Val = "20" }, new RunFonts { Ascii = "微软雅黑", EastAsia = "微软雅黑" }), new Text(c)))));
            tbl.Append(dRow);
        }
        body.Append(tbl);
    }
}
