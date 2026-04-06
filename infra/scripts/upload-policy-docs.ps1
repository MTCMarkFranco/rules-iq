<#
.SYNOPSIS
    Uploads policy documents to Azure Blob Storage.
.DESCRIPTION
    Handles the full lifecycle: temporarily enables public network access
    on the storage account, ensures the target container exists, uploads
    all PDFs, then re-disables public network access.
    Idempotent — safe to re-run at any time.
#>

param(
    [string]$ResourceGroup = "RG-OpenAI",
    [string]$StorageAccount = "sadatafileshubcanada",
    [string]$Container = "policy-documents",
    [string]$SourcePath = "$PSScriptRoot\..\..\meta-contracts\loan-eligibility",
    [string]$Pattern = "*.pdf"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$SourcePath = Resolve-Path $SourcePath

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "  Upload Policy Documents" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Storage Account : $StorageAccount"
Write-Host "Container       : $Container"
Write-Host "Source          : $SourcePath"
Write-Host "Pattern         : $Pattern"

# Check current public network access setting
$currentAccess = az storage account show `
    -n $StorageAccount `
    --resource-group $ResourceGroup `
    --query publicNetworkAccess `
    --output tsv

$networkWasDisabled = $currentAccess -eq "Disabled"

if ($networkWasDisabled) {
    Write-Host "`nPublic network access is disabled — enabling temporarily..." -ForegroundColor Yellow
    az storage account update `
        -n $StorageAccount `
        --resource-group $ResourceGroup `
        --public-network-access Enabled `
        --output none
    if ($LASTEXITCODE -ne 0) { throw "Failed to enable public network access" }
    Write-Host "Public network access enabled." -ForegroundColor Green
}

try {
    # Ensure container exists
    $exists = az storage container exists `
        --name $Container `
        --account-name $StorageAccount `
        --auth-mode login `
        --output tsv `
        --query exists

    if ($exists -ne "true") {
        Write-Host "`nCreating container '$Container'..." -ForegroundColor Yellow
        az storage container create `
            --name $Container `
            --account-name $StorageAccount `
            --auth-mode login `
            --output none
        if ($LASTEXITCODE -ne 0) { throw "Failed to create container" }
        Write-Host "Container created." -ForegroundColor Green
    } else {
        Write-Host "`nContainer '$Container' already exists." -ForegroundColor Green
    }

    # Upload blobs
    Write-Host "`nUploading files..." -ForegroundColor Yellow
    az storage blob upload-batch `
        --account-name $StorageAccount `
        --destination $Container `
        --source $SourcePath `
        --auth-mode login `
        --pattern $Pattern `
        --overwrite true `
        --output table

    if ($LASTEXITCODE -ne 0) { throw "Blob upload failed" }
    Write-Host "Upload completed." -ForegroundColor Green
}
finally {
    # Restore original network access setting
    if ($networkWasDisabled) {
        Write-Host "`nRe-disabling public network access..." -ForegroundColor Yellow
        az storage account update `
            -n $StorageAccount `
            --resource-group $ResourceGroup `
            --public-network-access Disabled `
            --output none
        Write-Host "Public network access disabled." -ForegroundColor Green
    }
}

Write-Host "`n================================================" -ForegroundColor Cyan
Write-Host "  Upload Complete!" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
