# NetPlan Git Bundle 同步工具
# D:\99AI应用\NetPlan\tools\NetPlan-Sync.ps1
param([switch]$Auto)
$ErrorActionPreference = "Stop"
$ProjectName = "NetPlan"
$BundleFile = "$ProjectName.bundle"
$LastSyncMarker = ".last_sync"

function Write-Header($text) {
    Write-Host ""
    Write-Host "=============================================" -ForegroundColor Cyan
    Write-Host "  $text" -ForegroundColor Cyan
    Write-Host "=============================================" -ForegroundColor Cyan
}
function Write-Success($text) { Write-Host "  [OK] $text" -ForegroundColor Green }
function Write-Warn($text)    { Write-Host "  [!] $text" -ForegroundColor Yellow }
function Write-Err($text)     { Write-Host "  [X] $text" -ForegroundColor Red }
function Write-Info($text)    { Write-Host "  > $text" -ForegroundColor Gray }

function Get-GitRoot($path) {
    $dir = $path
    while ($dir) {
        if (Test-Path "$dir\.git") { return $dir }
        $dir = Split-Path $dir -Parent
    }
    return $null
}

function Git-Init($gitRoot) {
    Push-Location $gitRoot
    git init 2>$null
    git add . 2>$null
    git commit -m "初始化: $ProjectName 项目" 2>$null
    Pop-Location
}

function Git-Status($gitRoot) {
    Push-Location $gitRoot
    $r = git status --porcelain 2>$null
    Pop-Location
    return $r
}

function Git-Commit-All($gitRoot, $msg) {
    Push-Location $gitRoot
    git add .
    git commit -m $msg 2>$null
    Pop-Location
    return ($LASTEXITCODE -eq 0)
}

function Git-Branch($gitRoot) {
    Push-Location $gitRoot
    $r = git rev-parse --abbrev-ref HEAD 2>$null
    Pop-Location
    return $r
}

function Git-Count($gitRoot) {
    Push-Location $gitRoot
    $r = git rev-list --count HEAD 2>$null
    Pop-Location
    return [int]$r
}

function Git-Bundle-All($gitRoot, $bundlePath) {
    Push-Location $gitRoot
    git bundle create $bundlePath --all 2>$null
    Pop-Location
    return ($LASTEXITCODE -eq 0)
}

function Git-Verify-Bundle($bundlePath) {
    git bundle list-heads $bundlePath 2>&1 | Out-Null
    return ($LASTEXITCODE -eq 0)
}

function Git-Pull($gitRoot, $bundlePath) {
    Push-Location $gitRoot
    git pull $bundlePath HEAD 2>$null
    Pop-Location
    return ($LASTEXITCODE -eq 0)
}

function Git-Clone($bundlePath, $targetDir) {
    git clone $bundlePath $targetDir 2>$null
    return ($LASTEXITCODE -eq 0)
}

function Get-AvailableDrives {
    Get-PSDrive -PSProvider FileSystem | Where-Object { $_.Name -match "^[A-Z]$" -and $_.Name -ne "C" }
}

function Select-Drive($prompt) {
    $drives = Get-AvailableDrives
    if ($drives.Count -eq 0) {
        Write-Err "未找到任何U盘/移动硬盘！"
        Write-Host "  请插入U盘后再试。" -ForegroundColor Yellow
        return $null
    }
    if ($drives.Count -eq 1) {
        $d = $drives[0]
        $freeGB = [Math]::Round($d.Free / 1GB, 1)
        Write-Info "已找到：$($d.Name):\ 剩余 ${freeGB} GB"
        return "$($d.Name):"
    }
    Write-Host ""
    Write-Host "  $prompt" -ForegroundColor Yellow
    $drives | ForEach-Object -Begin { $i = 1 } {
        $freeGB = [Math]::Round($_.Free / 1GB, 1)
        Write-Host "    $i) $($_.Name):\ 剩余 ${freeGB} GB" -ForegroundColor White
        $i++
    }
    if ($Auto) { return "$($drives[0].Name):" }
    do { $choice = Read-Host "  请选择 (1-$($drives.Count))" } while ($choice -notmatch '^\d+$' -or [int]$choice -lt 1 -or [int]$choice -gt $drives.Count)
    return "$($drives[[int]$choice-1].Name):"
}

function Ensure-Git-Identity {
    if ([string]::IsNullOrWhiteSpace((git config user.email 2>$null))) {
        Write-Warn "Git未配置用户名/邮箱，先设置一下（只需一次）"
        $email = Read-Host "  输入邮箱（如 you@example.com）"
        $name  = Read-Host "  输入名字（如 张三）"
        if (-not [string]::IsNullOrWhiteSpace($email)) { git config --global user.email $email }
        if (-not [string]::IsNullOrWhiteSpace($name))  { git config --global user.name  $name }
        Write-Success "Git身份已配置"
    }
}

function Backup-ToUsb($gitRoot, $usbRoot) {
    Write-Header "备份到U盘（Push）"
    if (-not (Test-Path "$gitRoot\.git")) {
        Write-Warn "当前目录还不是Git仓库，正在初始化..."
        Ensure-Git-Identity
        Git-Init $gitRoot
        Write-Success "Git仓库已初始化"
    }
    $status = Git-Status $gitRoot
    if ($status) {
        Write-Warn "检测到未提交的更改，正在自动提交..."
        Ensure-Git-Identity
        $msg = Read-Host "  输入提交说明（直接回车用默认）"
        if ([string]::IsNullOrWhiteSpace($msg)) { $msg = "自动同步：$(Get-Date -Format 'yyyy-MM-dd HH:mm')" }
        Git-Commit-All $gitRoot $msg
        Write-Success "更改已提交"
    }
    $branch = Git-Branch $gitRoot
    $count  = Git-Count $gitRoot
    $bundlePath = Join-Path $usbRoot $BundleFile
    $infoPath   = Join-Path $usbRoot "$ProjectName-同步信息.txt"
    Write-Info "正在创建Bundle文件..."
    Write-Info "  目标U盘：$usbRoot  分支：$branch  提交数：$count"
    $ok = Git-Bundle-All $gitRoot $bundlePath
    if (-not $ok) { Write-Err "Bundle创建失败！"; return $false }
    $sizeMB = [Math]::Round((Get-Item $bundlePath).Length / 1MB, 1)
    Write-Success "Bundle创建成功！大小：${sizeMB} MB"
    $syncInfo = "备份时间   : $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')\n项目路径   : $gitRoot\n当前分支   : $branch\n提交总数   : $count\nBundle大小 : ${sizeMB} MB"
    $syncInfo | Out-File -FilePath $infoPath -Encoding UTF8
    Write-Success "同步信息已保存到U盘"
    @{ Date = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'; Branch = $branch; Commits = $count; SizeMB = $sizeMB } | ConvertTo-Json | Out-File -FilePath (Join-Path $gitRoot $LastSyncMarker) -Encoding UTF8
    Write-Host ""
    Write-Host "  请把U盘带走，到另一台电脑上选【从U盘恢复】" -ForegroundColor Green
    return $true
}

function Restore-FromUsb($targetDir) {
    Write-Header "从U盘恢复（Pull）"
    $drives = Get-AvailableDrives
    $bundlePath = $null; $sourceDrive = $null
    foreach ($d in $drives) {
        $test = Join-Path "$($d.Name):" $BundleFile
        if (Test-Path $test) { $bundlePath = $test; $sourceDrive = "$($d.Name):"; break }
    }
    if (-not $bundlePath) {
        Write-Err "U盘中未找到 $BundleFile！"
        Write-Host "  请确认Bundle文件已放到U盘根目录。" -ForegroundColor Yellow
        return $false
    }
    $sizeMB = [Math]::Round((Get-Item $bundlePath).Length / 1MB, 1)
    Write-Info "找到Bundle：$bundlePath（${sizeMB} MB）"
    $infoPath = Join-Path $sourceDrive "$ProjectName-同步信息.txt"
    if (Test-Path $infoPath) {
        Write-Host ""
        Write-Host "  U盘备份信息：" -ForegroundColor Cyan
        Get-Content $infoPath | ForEach-Object { Write-Host "    $_" -ForegroundColor Gray }
    }
    if ((Test-Path $targetDir) -and (Get-ChildItem $targetDir -File -EA SilentlyContinue | Measure-Object).Count -gt 0) {
        Write-Warn "目标目录不为空：$targetDir"
        $confirm = if ($Auto) { "y" } else { Read-Host "  是否删除并重新克隆？(y/n)" }
        if ($confirm -ne "y") { Write-Info "已取消"; return $false }
        $backupDir = "$targetDir.backup_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
        Write-Info "原目录备份到：$backupDir"
        Move-Item $targetDir $backupDir -Force
    }
    $parentDir = Split-Path $targetDir -Parent
    if (-not (Test-Path $parentDir)) { New-Item -ItemType Directory -Path $parentDir -Force | Out-Null }
    Write-Info "正在克隆项目到：$targetDir"
    $ok = Git-Clone $bundlePath $targetDir
    if (-not $ok) { Write-Err "克隆失败！"; return $false }
    Write-Success "恢复成功！"
    Write-Host ""
    Write-Host "  项目位置：$targetDir" -ForegroundColor Green
    Write-Host "  在此目录下运行 dotnet build && dotnet run 即可启动" -ForegroundColor Gray
    @{ Date = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'; Action = "Restore"; From = $bundlePath; To = $targetDir } | ConvertTo-Json | Out-File -FilePath (Join-Path $targetDir $LastSyncMarker) -Encoding UTF8
    return $true
}

function Update-FromUsb($gitRoot, $usbRoot) {
    Write-Header "从U盘增量更新"
    $bundlePath = Join-Path $usbRoot $BundleFile
    if (-not (Test-Path $bundlePath)) {
        Write-Err "U盘中未找到Bundle文件，请先在其他电脑执行备份"
        return $false
    }
    Write-Info "验证Bundle..."
    if (-not (Git-Verify-Bundle $bundlePath)) { Write-Err "Bundle文件无效"; return $false }
    Write-Info "正在拉取更新..."
    $ok = Git-Pull $gitRoot $bundlePath
    if ($ok) { Write-Success "更新成功！" } else { Write-Warn "更新有冲突，请手动解决" }
    return $ok
}

function Show-Status($gitRoot, $usbRoot) {
    Write-Header "同步状态"
    $infoPath = Join-Path $usbRoot "$ProjectName-同步信息.txt"
    if (Test-Path $infoPath) {
        Write-Host ""
        Write-Host "  U盘备份信息：" -ForegroundColor Cyan
        Get-Content $infoPath | ForEach-Object { Write-Host "    $_" -ForegroundColor White }
    }
    $bundlePath = Join-Path $usbRoot $BundleFile
    if (Test-Path $bundlePath) {
        $sizeMB = [Math]::Round((Get-Item $bundlePath).Length / 1MB, 1)
        Write-Success "U盘Bundle文件存在：${sizeMB} MB"
    } else { Write-Warn "U盘中未找到Bundle文件" }
    if ($gitRoot) {
        Write-Host ""
        Write-Host "  本地Git状态：" -ForegroundColor Cyan
        Write-Info "仓库路径：$gitRoot"
        Write-Info "当前分支：$(Git-Branch $gitRoot)"
        Write-Info "提交总数：$(Git-Count $gitRoot)"
        $st = Git-Status $gitRoot
        if ($st) { Write-Warn "有 $(@($st).Count) 个未提交的文件" } else { Write-Success "工作区干净" }
        $sf = Join-Path $gitRoot $LastSyncMarker
        if (Test-Path $sf) { $ls = (Get-Content $sf | ConvertFrom-Json); Write-Info "上次同步：$($ls.Date)" }
    }
}

function Show-Menu {
    Write-Host ""
    Write-Host "  [1] 备份到U盘（Push）     <- 把代码存到U盘带走" -ForegroundColor Green
    Write-Host "  [2] 从U盘恢复（Pull）     <- 从U盘拉取代码到本机" -ForegroundColor Yellow
    Write-Host "  [3] 从U盘增量更新           <- 已恢复过的项目拉取最新版" -ForegroundColor Magenta
    Write-Host "  [4] 查看同步状态" -ForegroundColor Cyan
    Write-Host "  [Q] 退出" -ForegroundColor DarkGray
    Write-Host ""
}

# ==================== 主程序 ====================
Clear-Host
Write-Host ""
Write-Host "  =============================================" -ForegroundColor Cyan
Write-Host "     NetPlan Git Bundle 同步工具" -ForegroundColor Cyan
Write-Host "     支持任意工作目录 · 离线可用" -ForegroundColor Cyan
Write-Host "  =============================================" -ForegroundColor Cyan
$scriptRoot = $PSScriptRoot
$currentDir = $PWD.Path
$gitRoot    = Get-GitRoot $currentDir
if (-not $gitRoot) { $gitRoot = Get-GitRoot $scriptRoot }
if ($gitRoot) {
    Write-Info "已检测到项目：$gitRoot"
    if ($gitRoot -ne $currentDir) { Write-Info "切换到：$gitRoot"; Set-Location $gitRoot }
} else {
    Write-Warn "未找到Git仓库"
    Write-Host ""
    Write-Host "  [1] 初始化当前目录为Git仓库并备份到U盘" -ForegroundColor Green
    Write-Host "  [2] 从U盘恢复项目（需选择目标目录）" -ForegroundColor Yellow
    Write-Host "  [Q] 退出" -ForegroundColor DarkGray
    Write-Host ""
    if ($Auto) { exit 1 }
    $ch = Read-Host "  请选择"
    if ($ch -eq "q") { exit 0 }
    if ($ch -eq "2") {
        $t = Read-Host "  输入目标目录（回车=桌面\NetPlan）"
        if ([string]::IsNullOrWhiteSpace($t)) { $t = [Environment]::GetFolderPath("Desktop") + "\NetPlan" }
        $drv = Select-Drive "请选择U盘盘符："
        if ($drv) { Restore-FromUsb $t }
        if (-not $Auto) { Read-Host "  按回车退出" }
        exit 0
    }
}
$drv = Select-Drive "请选择U盘盘符："
if (-not $drv) { if (-not $Auto) { Read-Host "  按回车退出" }; exit 1 }
if ($gitRoot) { Set-Location $gitRoot }
if ($Auto) { Backup-ToUsb $gitRoot $drv; exit 0 }
while ($true) {
    Show-Menu
    $ch = Read-Host "  请选择操作（1/2/3/4/Q）"
    switch ($ch) {
        "1" { Backup-ToUsb $gitRoot $drv }
        "2" {
            $t = Read-Host "  目标目录（回车=桌面\NetPlan_Restored）"
            if ([string]::IsNullOrWhiteSpace($t)) { $t = (Split-Path $gitRoot -Parent) + "\NetPlan_Restored" }
            Restore-FromUsb $t
        }
        "3" { Update-FromUsb $gitRoot $drv }
        "4" { Show-Status $gitRoot $drv }
        { "q","Q" -contains $_ } {
            Write-Host ""
            Write-Host "  完毕！记得把Bundle文件从U盘带走~" -ForegroundColor Cyan
            exit 0
        }
        default { Write-Warn "无效选择，请输入1-4或Q" }
    }
    Write-Host ""
    if (-not $Auto) { Read-Host "  按回车继续" }
}
