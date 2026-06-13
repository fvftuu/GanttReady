## NetPlan 项目状态

### TS 重构 Phase 1 完成 (2026-05-15)
- TypeScript 17 模块骨架全部就位，`tsc --noEmit` 零错误
- **核心算法已迁移**: `calculateTimeParams`, `applySingleStartEnd`, `calculateVerticalLayout` 已从 legacy JS 精确转移到 `core/cpm.ts`, `core/layout.ts`
- **24 个测试全通过**（`node test/core-algorithms.js`）
- **构建管线就绪**: 源码 `netplan/*.ts` → tsc → esbuild → `netplan.js` (IIFE, ~10KB minified)
- 当前分支：`refactor/ts-rewrite`

### 下一步
- **Phase 2**: 迁移渲染模块（`render/`）—— timeline, network, arrows, crossarc, progress, legend
- **Phase 3**: 迁移交互模块（`interaction/`）—— panzoom, nodedrag, export
- **Phase 4**: 迁移甘特图/图表/存储（`gantt/`, `charts/`, `storage/`）
- **Phase 5**: 删除 legacy netplan.js 中已迁移的代码段

详见：[I:\My memory\NetPlan-TS重构架构设计.md](file:///I:/My%20memory/NetPlan-TS%E9%87%8D%E6%9E%84%E6%9E%B6%E6%9E%84%E8%AE%BE%E8%AE%A1.md)
