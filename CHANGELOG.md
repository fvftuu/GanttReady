## 2026-05-14

### Step 3: Experience Polish

- **Cross-arc improvements** — arcs now drawn on non-critical arrows at all crossings (previously skipped when both lines were critical). Fixed bounding box check from strict `<>` to inclusive `<= >=` so endpoint-touching segments are also detected. Fixed cross-product rejection from `>=0` to `>0` so collinear overlaps are separately handled.
- **Zoom compensation for node drag** — `deltaDays` calculation now reads SVG's CSS `scale()` transform and divides raw pixel distance by scale factor before computing day offset. Fixes drag offset drift when viewport is zoomed.
- **Updated CHANGELOG.md**
