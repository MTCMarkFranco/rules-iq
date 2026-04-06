<#
.SYNOPSIS
    Builds and deploys the indexer skill Web API to Azure App Service.
.DESCRIPTION
    Phase 3.6: Publishes the RulesIQ.IndexerSkill .NET 8 project and deploys
    the zip package to the App Service.
#>

param(
    [string]$ResourceGroup = "rg-rules-iq",
    [string]$AppName = "app-rulesiq-indexer-skill",
    [string]$ProjectPath = "$PSScriptRoot\..\..\src\indexer-skill\RulesIQ.IndexerSkill\RulesIQ.IndexerSkill.csproj"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

Write-Host "--- Phase 3.6: Build & Deploy Indexer Skill ---" -ForegroundColor Yellow

$publishDir = Join-Path $env:TEMP "rulesiq-publish"
$zipPath = Join-Path $env:TEMP "rulesiq-publish.zip"

try {
    # Build and publish
    Write-Host "Building and publishing $ProjectPath..."
    dotnet publish $ProjectPath -c Release -o $publishDir --nologo
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }
    Write-Host "Build succeeded." -ForegroundColor Green

    # Create zip
    Write-Host "Creating deployment package..."
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
    Compress-Archive -Path "$publishDir\*" -DestinationPath $zipPath -Force

    # Deploy
    Write-Host "Deploying to $AppName..."
    az webapp deploy `
        --name $AppName `
        --resource-group $ResourceGroup `
        --src-path $zipPath `
        --type zip
    if ($LASTEXITCODE -ne 0) { throw "az webapp deploy failed" }
    Write-Host "Deployment succeeded." -ForegroundColor Green

} finally {
    # Clean up temp files
    if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
}

Write-Host "`nIndexer skill deployed to https://$AppName.azurewebsites.net" -ForegroundColor Green
