# NetPlan Changelog

## 2026-05-14

### Step 2: Quality Infrastructure

- **Core algorithm unit tests** added at `test/core-algorithms.js`. 24 test cases covering:
  - `calculateTimeParams` — event merging, parallel/serial activities
  - `applySingleStartEnd` — virtual start/end node insertion
  - `calculateVerticalLayout` — BFS topology layer assignment
  - Run via: `node test/core-algorithms.js`
- **Node.js test compatibility** — netplan.js now has a shim at the top that mocks `window`/`document`/`navigator` for the Node.js test environment
- **Redundant scripts cleaned up** — 14 duplicate startup scripts moved to `archive/`. Only `run.bat` remains as the single entry point.

### Step 1: Core Fixes

- **Network progress line** → L-shaped arrow progress points now calculate correct Y coordinate (horizontal segment = src.y, vertical segment = interpolated)
- **Non-critical arrow color** → `#333` → `#1890ff` (blue, matching industry standard)
- **Dummy arrow color** → `#aaa` → `#1890ff` (blue dashed)
- **Non-critical node stroke** → red → `#1890ff` (blue, matching arrow color)
- **Progress date persistence** → saved to `localStorage`, survives page refresh
- **Async error handling** → all `invokeMethodAsync()` calls now have `.catch()` handlers
- **Syntax bug fix** → dangling `});` in node drag confirm block (could cause JS crash)
- **Validation scripts** → `scripts/netplan-check.ps1` / `.bat` with syntax check + build validation
