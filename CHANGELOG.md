## 2026-05-15

### TypeScript 重构 Phase 2 — 渲染模块迁移

- **render/network.ts** — renderNetwork 完整迁移（16KB），含节点拖拽、前锋线拖动、画布平移、横向滚动同步、自检函数
- **render/generator.ts** — buildNetworkSvg 简化版实现（14KB），生成含时标尺、箭线、箭头、事件节点、行背景、底部标尺的完整 SVG
- **index.ts** 改用真实 renderNetwork 替代 console.warn 占位
- 三关验证全部通过（tsc → 测试 → dotnet build）
- netplan.js 编译产出 25KB

### Phase 1 — 核心算法迁移

- **新建 TypeScript 模块架构** — 17 模块分解，`tsc --noEmit` 零错误
- **构建管线搭建** — TypeScript → esbuild → minified IIFE (`netplan.js`, ~10KB)
- **core/cpm.ts** — `calculateTimeParams`, `applySingleStartEnd` 从 legacy 304 行 JS 精确迁移
- **core/layout.ts** — `calculateVerticalLayout` 从 legacy 160 行精确迁移
- **24 个单元测试全部通过** — 保持与 legacy 完全相同的返回结构和事件命名
- `.gitignore` 排除 `dist/`, `temp/`, `node_modules/`
- 架构设计文档同步到 `I:\My memory\NetPlan-TS重构架构设计.md`

## 2026-05-14

### Step 3: Experience Polish

- **Cross-arc improvements** — arcs now drawn on non-critical arrows at all crossings (previously skipped when both lines were critical). Fixed bounding box check from strict `<>` to inclusive `<= >=` so endpoint-touching segments are also detected. Fixed cross-product rejection from `>=0` to `>0` so collinear overlaps are separately handled.
- **Zoom compensation for node drag** — `deltaDays` calculation now reads SVG's CSS `scale()` transform and divides raw pixel distance by scale factor before computing day offset. Fixes drag offset drift when viewport is zoomed.
- **Updated CHANGELOG.md**
