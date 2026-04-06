<#
.SYNOPSIS
    Master deployment script for Rules-IQ infrastructure.
.DESCRIPTION
    Deploys all Azure resources, configures existing services, and creates
    data-plane objects. Uses managed identity auth — no API keys.
#>

param(
    [string]$Location = "eastus",
    [string]$ResourceGroup = "rg-rules-iq",
    [string]$OpenAIResourceGroup = "RG-OpenAI",
    [switch]$SkipBicep,
    [switch]$SkipDataPlane,
    [switch]$SkipAgents
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "  Rules-IQ Infrastructure Deployment" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan

# Verify az cli login
$account = az account show --output json | ConvertFrom-Json
Write-Host "`nSubscription: $($account.name)" -ForegroundColor Green
Write-Host "User: $($account.user.name)" -ForegroundColor Green

# Phase 2: Configure Existing Services (must run before Bicep so AI Search
# system-assigned MI is available for RBAC assignments in Phase 4)
Write-Host "`n--- Phase 2: Configure Existing Services ---" -ForegroundColor Yellow
& "$PSScriptRoot\configure-search.ps1" -ResourceGroup $OpenAIResourceGroup

# Phase 1, 3, 4: Bicep Deployment
if (-not $SkipBicep) {
    Write-Host "`n--- Phase 1, 3, 4: Bicep Deployment ---" -ForegroundColor Yellow

    az deployment sub create `
        --location $Location `
        --template-file "$PSScriptRoot\..\main.bicep" `
        --parameters "$PSScriptRoot\..\main.bicepparam" `
        --name "rulesiq-$(Get-Date -Format 'yyyyMMdd-HHmmss')" `
        --verbose

    if ($LASTEXITCODE -ne 0) { throw "Bicep deployment failed" }
    Write-Host "Bicep deployment completed." -ForegroundColor Green
}

# Wait for RBAC propagation
Write-Host "`nWaiting 60 seconds for RBAC propagation..." -ForegroundColor Yellow
Start-Sleep -Seconds 60

# Phase 5: Data-Plane Objects
if (-not $SkipDataPlane) {
    Write-Host "`n--- Phase 5: Data-Plane Objects ---" -ForegroundColor Yellow
    & "$PSScriptRoot\create-index.ps1" -ResourceGroup $OpenAIResourceGroup
    & "$PSScriptRoot\create-indexer.ps1" -ResourceGroup $OpenAIResourceGroup
}

# Phase 6: AI Foundry Agents
if (-not $SkipAgents) {
    Write-Host "`n--- Phase 6: AI Foundry Agents ---" -ForegroundColor Yellow
    & "$PSScriptRoot\create-agents.ps1" -ResourceGroup $ResourceGroup
}

Write-Host "`n================================================" -ForegroundColor Cyan
Write-Host "  Deployment Complete!" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
