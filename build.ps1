#Requires -Version 7.0
<#
    构建全部发布产物到 dist/。

    用法:
        pwsh build.ps1                     # 全部
        pwsh build.ps1 -Target msi,linux    # 只构建指定项
        pwsh build.ps1 -Version 1.2.0       # 指定传给 MSI 的版本号（默认读 csproj）
#>
[CmdletBinding()]
param(
    [ValidateSet('portable', 'single', 'msi', 'linux')]
    [string[]]$Target = @('portable', 'single', 'msi', 'linux'),

    [string]$Version,
    [string]$OutputDir = 'dist'
)

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

$project = Join-Path $PSScriptRoot 'Toashot.csproj'
$outDir  = Join-Path $PSScriptRoot $OutputDir
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

if (-not $Version) {
    $props = dotnet msbuild $project -nologo -getProperty:Version
    $Version = ($props | Select-Object -First 1).Trim()
}

# 单文件发布时原生库的 pdb 会被一并复制（含 runtimes\ 下的各平台目录），DebugType=none 拦不住
function Publish {
    param([string]$Name, [string[]]$ExtraArgs = @())

    $out = Join-Path $outDir $Name
    dotnet publish $project -c Release -p:DebugType=none -p:DebugSymbols=false -o $out -v m @ExtraArgs
    if ($LASTEXITCODE -ne 0) { throw "publish $Name 失败" }
    Get-ChildItem $out -Recurse -Filter *.pdb | Remove-Item -Force
    Write-Host "[ok] $out" -ForegroundColor Green
}

if ($Target -contains 'portable') {
    # 框架依赖：exe + 一堆 dll，需要已安装 .NET 10 Desktop Runtime
    Publish 'portable' @('-r:win-x64', '--self-contained', 'false')
}

if ($Target -contains 'single') {
    # win-x64 自包含单文件，双击即用
    Publish 'win-x64-single' @(
        '-r:win-x64', '--self-contained', 'true',
        '-p:PublishSingleFile=true',
        '-p:IncludeNativeLibrariesForSelfExtract=true',
        '-p:EnableCompressionInSingleFile=true'
    )
}

if ($Target -contains 'msi') {
    # 安装包内容用自包含多文件版（无需目标机装运行时），装到 %LOCALAPPDATA%\Programs
    Publish 'win-x64-app' @('-r:win-x64', '--self-contained', 'true')

    wix build (Join-Path $PSScriptRoot 'installer\Toashot.wxs') `
        -arch x64 `
        -d "ProductVersion=$Version" `
        -d "ProjectDir=$PSScriptRoot\" `
        -d "PublishDir=$outDir\win-x64-app\" `
        -o (Join-Path $outDir 'Toashot.msi') `
        -pdbtype none
    if ($LASTEXITCODE -ne 0) { throw 'wix build 失败' }
    Write-Host "[ok] $(Join-Path $outDir 'Toashot.msi')  (v$Version)" -ForegroundColor Green
}

if ($Target -contains 'linux') {
    # 仅能编译通过：全局钩子/截屏依赖 Win32，运行时不可用
    Publish 'linux-x64' @('-r:linux-x64', '--self-contained', 'false')
}
