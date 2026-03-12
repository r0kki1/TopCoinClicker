$ErrorActionPreference = "Stop"

$buildDir = "C:\Users\r0kki\TopCoinClicker\Builds"
$targets = @(
    "D3D12",
    "MonoBleedingEdge",
    "TopCoinClicker_BurstDebugInformation_DoNotShip",
    "TopCoinClicker_Data",
    "TopCoinClicker.exe",
    "UnityCrashHandler64.exe",
    "UnityPlayer.dll"
)

foreach ($name in $targets) {
    $path = Join-Path $buildDir $name
    if (Test-Path $path) {
        Remove-Item $path -Recurse -Force
    }
}
