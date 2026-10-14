$taskRoot = 'D:\Programming\Comet_Foundry\project_CF'
$playerPath = Join-Path $taskRoot 'Builds\CometFoundry\CometFoundry.exe'
$reviewRoot = Join-Path $taskRoot 'ValidationArtifacts\Resolution-2026-10-14\Final'
$resolutions = @(@(1024,768), @(1280,720), @(1366,768), @(1280,1024), @(1920,1080), @(2560,1440), @(3440,1440))
$rows = @()
Add-Type -AssemblyName System.Drawing
foreach ($resolution in $resolutions) {
    $label = "$($resolution[0])x$($resolution[1])"
    $directory = Join-Path $reviewRoot $label
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
    $logPath = Join-Path $reviewRoot ($label+'-player.log')
    $processInfo = New-Object System.Diagnostics.ProcessStartInfo
    $processInfo.FileName = $playerPath
    $processInfo.UseShellExecute = $false
    # A displayed game window is required for Unity's swapchain screenshot.
    $processInfo.Arguments = '-screen-fullscreen 0 -screen-width '+$resolution[0]+' -screen-height '+$resolution[1]+' -force-d3d11 -foundryResolutionCapture "'+$directory+'" -logFile "'+$logPath+'"'
    $process = [System.Diagnostics.Process]::Start($processInfo)
    if (!$process.WaitForExit(120000)) {
        $process.Kill()
        throw "Review player timed out: $label"
    }
    $exitCode = $process.ExitCode
    $layoutPath = Join-Path $directory 'layout.tsv'
    if ($exitCode -ne 0 -or !(Test-Path $layoutPath)) { throw "Review player failed: $label, exit $exitCode" }
    $layout = Import-Csv -LiteralPath $layoutPath -Delimiter "`t"
    $images = Get-ChildItem -LiteralPath $directory -Filter '*.png'
    $image = [Drawing.Bitmap]::FromFile((Join-Path $directory '03-board.png'))
    $actualSize = "$($image.Width)x$($image.Height)"
    $colors = [Collections.Generic.HashSet[int]]::new()
    for ($y=0; $y -lt $image.Height; $y+=37) { for($x=0; $x -lt $image.Width; $x+=37) { $null=$colors.Add($image.GetPixel($x,$y).ToArgb()) } }
    $image.Dispose()
    $textWarnings = @(Get-Content (Join-Path $directory 'text-fit-warnings.tsv'))
    $geometryFailures = @($layout | Where-Object { $_.'guide-geometry'.StartsWith('FAIL') }).Count
    $errors = @(Select-String -LiteralPath $logPath -Pattern 'Exception|Assertion failed|Resolution guide geometry failed|NullReference|IndexOutOfRange').Count
    $rows += [pscustomobject]@{ Requested=$label; Actual=$actualSize; Frames=$layout.Count; Scale=$layout[0].scale; SmallTextPixels=$layout[0].'small-text-pixels'; NonBlankSampleColors=$colors.Count; TextWarnings=$textWarnings.Count; GuideFailures=$geometryFailures; RuntimeErrors=$errors; ExitCode=$exitCode }
    Write-Output ($rows[-1] | ConvertTo-Json -Compress)
}
$rows | Export-Csv -LiteralPath (Join-Path $reviewRoot 'summary.csv') -NoTypeInformation -Encoding UTF8
