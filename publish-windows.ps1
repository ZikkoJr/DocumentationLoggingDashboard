$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectPath = Join-Path $scriptRoot "DocumentationLoggingDashboard\DocumentationLoggingDashboard.csproj"
$outputFolder = Join-Path $scriptRoot "PublishedApp\win-x64"
$exePath = Join-Path $outputFolder "DocumentationLoggingDashboard.exe"

Write-Host "Publishing Documentation Logging Dashboard for Windows x64..."
Write-Host "Project: $projectPath"
Write-Host "Output folder: $outputFolder"
Write-Host ""

dotnet publish $projectPath -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o $outputFolder

Write-Host ""
Write-Host "Publish complete."
Write-Host "Final folder path: $outputFolder"
Write-Host "Expected .exe path: $exePath"
Write-Host "Double-click the .exe to launch the dashboard."
Write-Host "You can create a desktop shortcut to this .exe for everyday use."
