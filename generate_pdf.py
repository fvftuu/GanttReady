#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
NetPlan 项目功能概述与价值评估报告 - PDF生成器
"""

from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.lib.units import cm
from reportlab.platypus import SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle, PageBreak, HRFlowable
from reportlab.lib import colors
from reportlab.lib.enums import TA_CENTER, TA_LEFT, TA_RIGHT
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
import os

# 注册中文字体（使用系统字体）
try:
    pdfmetrics.registerFont(TTFont('SimSun', 'C:/Windows/Fonts/simsun.ttc'))
    pdfmetrics.registerFont(TTFont('SimSun-Bold', 'C:/Windows/Fonts/simsun.ttc'))
    CHINESE_FONT = 'SimSun'
    CHINESE_FONT_BOLD = 'SimSun-Bold'
except:
    CHINESE_FONT = 'Helvetica'
    CHINESE_FONT_BOLD = 'Helvetica-Bold'

# 页面设置
PAGE_WIDTH, PAGE_HEIGHT = A4
MARGIN_LEFT = 2.5*cm
MARGIN_RIGHT = 2.5*cm
MARGIN_TOP = 2.5*cm
MARGIN_BOTTOM = 2*cm

def create_styles():
    """创建样式表"""
    styles = getSampleStyleSheet()
    
    # 中文标题样式
    styles.add(ParagraphStyle(
        name='ChineseTitle',
        fontName=CHINESE_FONT_BOLD,
        fontSize=24,
        leading=30,
        alignment=TA_CENTER,
        spaceAfter=20,
    ))
    
    # 中文一级标题
    styles.add(ParagraphStyle(
        name='ChineseHeading1',
        fontName=CHINESE_FONT_BOLD,
        fontSize=18,
        leading=24,
        spaceBefore=20,
        spaceAfter=10,
    ))
    
    # 中文二级标题
    styles.add(ParagraphStyle(
        name='ChineseHeading2',
        fontName=CHINESE_FONT_BOLD,
        fontSize=14,
        leading=20,
        spaceBefore=15,
        spaceAfter=8,
    ))
    
    # 中文正文
    styles.add(ParagraphStyle(
        name='ChineseNormal',
        fontName=CHINESE_FONT,
        fontSize=11,
        leading=18,
        spaceAfter=6,
    ))
    
    # 中文列表
    styles.add(ParagraphStyle(
        name='ChineseBullet',
        fontName=CHINESE_FONT,
        fontSize=11,
        leading=18,
        leftIndent=20,
        spaceAfter=3,
    ))
    
    return styles

def create_table(data, col_widths=None):
    """创建表格"""
    if not data:
        return None
    
    table = Table(data, colWidths=col_widths)
    style = TableStyle([
        ('FONTNAME', (0, 0), (-1, -1), CHINESE_FONT),
        ('FONTSIZE', (0, 0), (-1, -1), 10),
        ('BACKGROUND', (0, 0), (-1, 0), colors.grey),
        ('TEXTCOLOR', (0, 0), (-1, 0), colors.whitesmoke),
        ('ALIGN', (0, 0), (-1, -1), 'LEFT'),
        ('VALIGN', (0, 0), (-1, -1), 'MIDDLE'),
        ('GRID', (0, 0), (-1, -1), 1, colors.black),
        ('ROWBACKGROUNDS', (0, 1), (-1, -1), [colors.white, colors.lightgrey]),
    ])
    table.setStyle(style)
    return table

def build_document(filename='NetPlan功能概述与价值评估.pdf'):
    """构建PDF文档"""
    doc = SimpleDocTemplate(
        filename,
        pagesize=A4,
        leftMargin=MARGIN_LEFT,
        rightMargin=MARGIN_RIGHT,
        topMargin=MARGIN_TOP,
        bottomMargin=MARGIN_BOTTOM,
    )
    
    styles = create_styles()
    story = []
    
    # 封面页
    story.append(Spacer(1, 5*cm))
    story.append(Paragraph('NetPlan 项目功能概述', styles['ChineseTitle']))
    story.append(Paragraph('与价值评估报告', styles['ChineseTitle']))
    story.append(Spacer(1, 2*cm))
    
    info_data = [
        ['文档版本', 'V1.0'],
        ['编制日期', '2026年5月10日'],
        ['项目阶段', '开发中期（V6.1功能规划中）'],
        ['编写目的', '供第三方机构/投资人/合作伙伴进行项目价值评估'],
    ]
    info_table = create_table(info_data, col_widths=[4*cm, 8*cm])
    story.append(info_table)
    story.append(PageBreak())
    
    # 目录（简化版）
    story.append(Paragraph('目录', styles['ChineseHeading1']))
    toc_items = [
        '一、项目概述',
        '二、技术架构',
        '三、功能模块详解',
        '四、竞争分析',
        '五、商业模式',
        '六、项目现状与成熟度评估',
        '七、发展路线图',
        '八、风险评估与应对策略',
        '九、总结与建议',
    ]
    for item in toc_items:
        story.append(Paragraph(f'  {item}', styles['ChineseNormal']))
    story.append(PageBreak())
    
    # 一、项目概述
    story.append(Paragraph('一、项目概述', styles['ChineseHeading1']))
    
    story.append(Paragraph('1.1 项目定位', styles['ChineseHeading2']))
    story.append(Paragraph(
        'NetPlan 是一款开源的网络计划编制与进度管理软件，专注于中国国标 GB/T 13400 的双代号网络图（AOA）实现，填补了开源项目管理工具在国标AOA网络图领域的空白。',
        styles['ChineseNormal']
    ))
    
    story.append(Paragraph('1.2 核心价值主张', styles['ChineseHeading2']))
    value_data = [
        ['价值点', '说明'],
        ['填补市场空白', '目前没有开源项目完整实现 GB/T 13400 标准的双代号网络图（AOA）'],
        ['现代化Web实现', '传统软件（维达/梦龙/同望）都是桌面软件且界面陈旧，NetPlan 用 Blazor Server 实现现代化Web应用'],
        ['成本优势', '相比 P6（数万元/年）和广联达（昂贵），NetPlan 开源免费'],
        ['技术栈优势', '.NET 8 + Blazor Server，企业级，易部署，易扩展'],
    ]
    story.append(create_table(value_data))
    story.append(Spacer(1, 0.5*cm))
    
    # 二、技术架构
    story.append(Paragraph('二、技术架构', styles['ChineseHeading1']))
    
    story.append(Paragraph('2.1 技术栈', styles['ChineseHeading2']))
    tech_data = [
        ['层级', '技术', '说明'],
        ['前端', 'Blazor Server (.NET 8)', '服务端渲染，实时UI更新（SignalR）'],
        ['后端', 'C# .NET 8', '业务逻辑层，CPM引擎'],
        ['数据库', 'SQLite', '轻量级，易部署，适合中小型项目'],
        ['图表渲染', '原生JavaScript + SVG', '甘特图 + 时标双代号网络图'],
        ['可视化', 'Chart.js', '资源分析图表'],
        ['文档处理', 'DocumentFormat.OpenXml 2.16.0', 'Excel导入导出'],
    ]
    story.append(create_table(tech_data))
    story.append(Spacer(1, 0.5*cm))
    
    # 三、功能模块（简化）
    story.append(Paragraph('三、功能模块详解', styles['ChineseHeading1']))
    
    story.append(Paragraph('3.1 甘特图模块', styles['ChineseHeading2']))
    gantt_features = [
        '✅ 冻结窗格：左侧任务列表固定，右侧甘特图可横向滚动',
        '✅ 时间标尺：双层结构，根据 dayWidth 自动切换日/周/月模式',
        '✅ 周末高亮：周六日灰底(#f0f0f0) + 灰字(#999)',
        '✅ 任务交互：双击编辑、拖拽调整日期、任务选中高亮',
        '✅ 条形图颜色规则（5种状态）：未开始(蓝)、进行中(橙)、已完成(绿)、关键(红)、里程碑(紫)',
        '✅ 缩放控制：+ / - 按钮，每次5%幅度，范围5%-1500%',
    ]
    for feature in gantt_features:
        story.append(Paragraph(feature, styles['ChineseBullet']))
    story.append(Spacer(1, 0.5*cm))
    
    story.append(Paragraph('3.2 时标双代号网络图模块', styles['ChineseHeading2']))
    network_features = [
        '✅ 国标AOA网络图：事件按时间点合并，工作箭线+虚工作箭线',
        '✅ 关键路径高亮：红节点 + 红边',
        '✅ 时差可视化：三行节点显示（ES|工期|EF / 代号 / LS|TF|LF）',
        '✅ 前锋线（JGJ/T121-2015 进度评估）：可拖动，进度评估着色',
        '✅ 今日线：红色虚线，显示当前日期位置',
        '✅ 节点交互：双击编辑、拖拽调整位置、拖拽角色区分',
    ]
    for feature in network_features:
        story.append(Paragraph(feature, styles['ChineseBullet']))
    story.append(Spacer(1, 0.5*cm))
    
    # 四、竞争分析（简化）
    story.append(Paragraph('四、竞争分析', styles['ChineseHeading1']))
    
    story.append(Paragraph('4.1 竞争格局矩阵', styles['ChineseHeading2']))
    competition_data = [
        ['维度', 'NetPlan', '维达/梦龙', 'P6/MS Project', '广联达斑马'],
        ['AOA双代号', '✅ 完整实现', '✅ 支持（桌面旧版）', '❌ 不支持', '✅ 支持'],
        ['Web化/云原生', '✅ Blazor Server', '❌ 桌面软件', '⚠️ P6有云版但贵', '✅ 有云版'],
        ['开源/可定制', '✅ 开源(.NET)', '❌ 闭源商业', '❌ 昂贵商业', '❌ 闭源商业'],
        ['价格', '✅ 开源免费', '💰 商业授权', '💰💰 昂贵', '💰💰 昂贵'],
        ['国标合规', '✅ GB/T 13400', '✅ 合规', '❌ 不合规', '✅ 合规'],
    ]
    story.append(create_table(competition_data))
    story.append(Spacer(1, 0.5*cm))
    
    # 五、商业模式
    story.append(Paragraph('五、商业模式', styles['ChineseHeading1']))
    
    story.append(Paragraph('5.1 推荐模式：开源核心 + 企业版授权', styles['ChineseHeading2']))
    business_data = [
        ['版本', '内容', '收费'],
        ['社区版（开源）', '甘特图 + AOA网络图 + CPM引擎 + 资源管理', '免费开源'],
        ['企业版', '社区版 + 多用户权限 + 审批流程 + 高级报表 + API接口 + SaaS部署', '按用户/年收费'],
    ]
    story.append(create_table(business_data))
    story.append(Spacer(1, 0.5*cm))
    
    story.append(Paragraph('5.2 盈利方式', styles['ChineseHeading2']))
    revenue_data = [
        ['盈利方式', '收费模式', '说明'],
        ['企业版授权费', '990-1990元/用户/年', '主要收入来源'],
        ['技术支持服务', '包含在授权费中或+50-100%', '稳定收入'],
        ['私有化部署服务', '5万-50万（一次性）', '高利润'],
        ['培训与认证', '99-499元/人', '品牌价值'],
        ['定制开发服务', '按人天收费（2000-5000元/人天）', '高利润'],
    ]
    story.append(create_table(revenue_data))
    story.append(Spacer(1, 0.5*cm))
    
    # 六、项目现状评估
    story.append(Paragraph('六、项目现状与成熟度评估', styles['ChineseHeading1']))
    
    story.append(Paragraph('6.1 代码质量评估', styles['ChineseHeading2']))
    evaluation_data = [
        ['评估维度', '评分(1-10)', '说明'],
        ['技术架构', '7/10', 'Blazor Server + .NET 8 现代，但 netplan.js 需重构'],
        ['功能完整性', '4/10', '核心功能有，但V6.1清单大量待实现'],
        ['独特性价值', '9/10', 'AOA国标开源实现，市场空白'],
        ['代码质量', '6/10', '有技债（大文件、备份堆积），但整体可维护'],
        ['用户体验', '5/10', '功能可用，但交互细节待打磨'],
        ['市场竞争力', '8/10', '开源 + 国标AOA = 强差异化'],
        ['综合评分', '6.5/10', '有潜力的种子项目，需补强功能和代码质量'],
    ]
    story.append(create_table(evaluation_data))
    story.append(Spacer(1, 0.5*cm))
    
    # 七、发展路线图
    story.append(Paragraph('七、发展路线图', styles['ChineseHeading1']))
    
    story.append(Paragraph('7.1 短期（1-3个月）— 产品完善', styles['ChineseHeading2']))
    short_term = [
        '📌 优先完成C1（时标双代号图增加工作）— 让软件真正可用',
        '📌 完成C系列功能（C2-C6）— 核心编制功能',
        '📌 发布到GitHub开源 — 用"国标AOA网络图开源实现"作为核心卖点',
        '📌 补充单元测试 — 提升代码质量',
        '📌 写用户手册 — 至少维达软件用户能快速上手',
    ]
    for item in short_term:
        story.append(Paragraph(item, styles['ChineseBullet']))
    story.append(Spacer(1, 0.5*cm))
    
    story.append(Paragraph('7.2 中期（3-6个月）— 商业化试点', styles['ChineseHeading2']))
    mid_term = [
        '📌 推出企业版 — 多用户 + 权限管理 + 高级报表',
        '📌 建立销售渠道 — 官网 + 代理商',
        '📌 签约首批付费客户（目标10家）',
        '📌 提供标准技术支持服务',
    ]
    for item in mid_term:
        story.append(Paragraph(item, styles['ChineseBullet']))
    story.append(Spacer(1, 0.5*cm))
    
    # 八、风险评估
    story.append(Paragraph('八、风险评估与应对策略', styles['ChineseHeading1']))
    
    story.append(Paragraph('8.1 技术风险', styles['ChineseHeading2']))
    tech_risk_data = [
        ['风险', '影响', '应对策略'],
        ['netplan.js 过大，难以维护', '🔴 高', '拆分为多个模块'],
        ['性能问题（1000+任务）', '🟡 中', '虚拟滚动、Web Worker、Canvas渲染'],
        ['浏览器兼容性', '🟡 中', '测试主流浏览器，添加polyfill'],
    ]
    story.append(create_table(tech_risk_data))
    story.append(Spacer(1, 0.5*cm))
    
    story.append(Paragraph('8.2 市场风险', styles['ChineseHeading2']))
    market_risk_data = [
        ['风险', '影响', '应对策略'],
        ['维达/广联达正面竞争', '🔴 高', '差异化：开源+免费+可定制，专注中小企业'],
        ['用户习惯迁移难', '🟡 中', '提供维达/梦龙数据导入工具'],
        ['开源社区不活跃', '🟡 中', '提供完善文档、教学视频、案例库'],
    ]
    story.append(create_table(market_risk_data))
    story.append(Spacer(1, 0.5*cm))
    
    # 九、总结
    story.append(Paragraph('九、总结与建议', styles['ChineseHeading1']))
    
    story.append(Paragraph('9.1 项目优势', styles['ChineseHeading2']))
    advantages = [
        '✅ 填补市场空白：国标AOA网络图的开源实现（独家）',
        '✅ 技术栈现代：Blazor Server + .NET 8，企业级，易部署',
        '✅ 成本优势明显：开源免费，相比P6/广联达成本降低90%+',
        '✅ 核心算法完整：CPM引擎 + 手动排程保护 + 资源平衡',
        '✅ 已有一定基础：甘特图/网络图/资源管理核心功能已实现',
    ]
    for adv in advantages:
        story.append(Paragraph(adv, styles['ChineseBullet']))
    story.append(Spacer(1, 0.5*cm))
    
    story.append(Paragraph('9.2 项目劣势', styles['ChineseHeading2']))
    disadvantages = [
        '❌ 功能不完整：V6.1清单中大量功能待实现',
        '❌ 代码质量有待提升：大文件（netplan.js 1904行，Gantt.razor 1977行）',
        '❌ 单元测试覆盖低：仅覆盖ScheduleEngine',
        '❌ 缺少文档和教程：无用户手册、视频教程、案例库',
        '❌ 未建立社区：未发布到GitHub，无社区贡献',
    ]
    for disadv in disadvantages:
        story.append(Paragraph(disadv, styles['ChineseBullet']))
    story.append(Spacer(1, 0.5*cm))
    
    story.append(Paragraph('9.3 核心建议', styles['ChineseHeading2']))
    suggestions = [
        '📌 优先完成C1（时标双代号图增加工作）— 让软件真正可用',
        '📌 发布到GitHub开源 — 用"国标AOA网络图开源实现"作为核心卖点',
        '📌 补充单元测试 — 提升代码质量',
        '📌 写用户手册 — 至少维达软件用户能快速上手',
        '📌 重构大文件 — 拆分 netplan.js 和 Gantt.razor',
    ]
    for sug in suggestions:
        story.append(Paragraph(sug, styles['ChineseBullet']))
    story.append(Spacer(1, 1*cm))
    
    # 结尾
    story.append(HRFlowable(width="100%", thickness=1, color=colors.grey))
    story.append(Spacer(1, 0.5*cm))
    story.append(Paragraph(
        '本报告基于2026年5月10日的项目现状编制，功能清单和路线图可能随开发进展调整。',
        styles['ChineseNormal']
    ))
    
    # 生成PDF
    doc.build(story)
    print(f'PDF文档已生成：{filename}')

if __name__ == '__main__':
    import sys
    filename = sys.argv[1] if len(sys.argv) > 1 else 'NetPlan功能概述与价值评估.pdf'
    build_document(filename)
