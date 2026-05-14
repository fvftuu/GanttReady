# NetPlan Pre/Post-Modification Validation Script
# Usage: .\scripts\netplan-check.ps1 [-Mode pre|post]

param([ValidateSet('pre','post')][string]$Mode = 'pre')

$JS = "I:\NetPlan\src\NetPlan.Server\wwwroot\js\netplan.js"
$BuildDir = "I:\NetPlan\src\NetPlan.Server"
$Root = "I:\NetPlan"

Write-Host "=== NetPlan Validation Check ($Mode) ===" -ForegroundColor Cyan
Write-Host ""

# 1. Syntax check
Write-Host "[1/4] Syntax check (node --check)..." -ForegroundColor Yellow
try {
    $result = node --check $JS 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "FAIL: netplan.js has syntax errors!" -ForegroundColor Red
        Write-Host $result
        exit 1
    }
    Write-Host "  PASS: netplan.js syntax OK" -ForegroundColor Green
} catch {
    Write-Host "FAIL: node --check threw exception: $_" -ForegroundColor Red
    exit 1
}

# 2. Key function presence check
Write-Host "[2/4] Key function check..." -ForegroundColor Yellow
$funcs = @(
    "calculateTimeParams",
    "buildNetworkSvg",
    "renderNetwork",
    "applySingleStartEnd",
    "calculateVerticalLayout",
    "validateNetworkRender"
)
foreach ($f in $funcs) {
    $found = Select-String -Path $JS -Pattern "function $f" -Quiet
    if ($found) {
        Write-Host "  OK: function $f found" -ForegroundColor Gray
    } else {
        Write-Host "  WARN: function $f NOT found" -ForegroundColor Yellow
    }
}

# 3. Build check (post-mode only -- too slow for pre)
if ($Mode -eq 'post') {
    Write-Host "[3/4] Dotnet build check..." -ForegroundColor Yellow
    $buildResult = dotnet build $BuildDir --no-restore 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) {
        Write-Host "FAIL: Build has errors!" -ForegroundColor Red
        $buildResult | Select-String "error"
        exit 1
    }
    Write-Host "  PASS: Build clean" -ForegroundColor Green
} else {
    Write-Host "[3/4] Build check skipped (use -Mode post)" -ForegroundColor DarkGray
}

# 4. Git status
Write-Host "[4/4] Git status..." -ForegroundColor Yellow
Set-Location $Root
git status --short
Write-Host ""

if ($Mode -eq 'post') {
    Write-Host "=== Post-fix check complete ===" -ForegroundColor Cyan
} else {
    Write-Host "=== Pre-fix check complete - ready to work ===" -ForegroundColor Cyan
}
