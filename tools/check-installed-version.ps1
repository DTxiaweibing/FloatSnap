<#
打包前置检查：新版本号必须大于本机已安装的版本。

原因：MSI 里配置了 MajorUpgrade，安装旧版本会被 Windows Installer 拒绝（退出码 1638）。
与其打完包才发现装不上，不如在打包前就拦住。

退出码：0 = 可以打包；1 = 版本号不够新（或无法解析）。
#>
param(
    [Parameter(Mandatory = $true)][string]$NewVersion
)

$ErrorActionPreference = 'Stop'

function ConvertTo-FourPartVersion([string]$text) {
    if ([string]::IsNullOrWhiteSpace($text)) { return $null }
    $parsed = $null
    if (-not [version]::TryParse($text.Trim(), [ref]$parsed)) { return $null }
    return [version]::new(
        $parsed.Major,
        $parsed.Minor,
        [Math]::Max($parsed.Build, 0),
        [Math]::Max($parsed.Revision, 0))
}

$paths = @(
    'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*',
    'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*'
)

$installed = Get-ItemProperty $paths -ErrorAction SilentlyContinue |
    Where-Object { $_.DisplayName -eq 'FloatSnap' } |
    ForEach-Object { ConvertTo-FourPartVersion $_.DisplayVersion } |
    Where-Object { $_ } |
    Sort-Object -Descending |
    Select-Object -First 1

$new = ConvertTo-FourPartVersion $NewVersion
if (-not $new) {
    Write-Error "无法解析版本号：$NewVersion"
    exit 1
}

if (-not $installed) { exit 0 }   # 本机没装过，随便打

if ($new -le $installed) {
    [Console]::Error.WriteLine("new=$new installed=$installed")
    exit 1
}

exit 0
