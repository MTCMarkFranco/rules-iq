<#
.SYNOPSIS
    Creates Entra ID app registration and configures Easy Auth v2 on the App Service.
.DESCRIPTION
    Phase 3.5: Sets up the authentication chain so AI Search's system-assigned
    managed identity can call the custom WebApiSkill on the App Service.
    1. Creates an Entra ID app registration (idempotent — skips if exists)
    2. Sets the identifier URI to api://{appId}
    3. Creates a service principal
    4. Configures Easy Auth v2 on the App Service with v1 issuer + dual audiences
    5. Restarts the App Service to apply changes
#>

param(
    [string]$ResourceGroup = "rg-rules-iq",
    [string]$AppName = "app-rulesiq-indexer-skill"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

Write-Host "--- Phase 3.5: App Registration & Easy Auth ---" -ForegroundColor Yellow

# Get tenant ID from current account
$tenantId = (az account show --query tenantId -o tsv)
Write-Host "Tenant ID: $tenantId"

# Check if app registration already exists
$existingAppId = az ad app list --display-name $AppName --query "[0].appId" -o tsv 2>$null

if ($existingAppId) {
    Write-Host "App registration already exists: $existingAppId" -ForegroundColor Green
    $appId = $existingAppId
} else {
    Write-Host "Creating app registration: $AppName"
    $appId = az ad app create --display-name $AppName --query appId -o tsv
    if ($LASTEXITCODE -ne 0) { throw "Failed to create app registration" }
    Write-Host "Created app registration: $appId" -ForegroundColor Green
}

# Set identifier URI (idempotent)
Write-Host "Setting identifier URI: api://$appId"
az ad app update --id $appId --identifier-uris "api://$appId"
if ($LASTEXITCODE -ne 0) { throw "Failed to set identifier URI" }

# Create service principal (idempotent — will fail silently if exists)
Write-Host "Ensuring service principal exists..."
az ad sp create --id $appId 2>$null
Write-Host "Service principal ready." -ForegroundColor Green

# Configure Easy Auth v2
Write-Host "Configuring Easy Auth v2 on $AppName..."
az webapp auth update `
    --name $AppName `
    --resource-group $ResourceGroup `
    --enabled true `
    --action LoginWithAzureActiveDirectory `
    --aad-client-id $appId `
    --aad-issuer "https://sts.windows.net/$tenantId/" `
    --aad-allowed-token-audiences "api://$appId" "$appId"

if ($LASTEXITCODE -ne 0) { throw "Failed to configure Easy Auth" }
Write-Host "Easy Auth v2 configured." -ForegroundColor Green

# Restart App Service to apply auth changes
Write-Host "Restarting App Service to apply Easy Auth..."
az webapp restart --name $AppName --resource-group $ResourceGroup
if ($LASTEXITCODE -ne 0) { throw "Failed to restart App Service" }
Write-Host "App Service restarted." -ForegroundColor Green

Write-Host "`nApp Registration & Easy Auth setup complete." -ForegroundColor Green
Write-Host "  App ID: $appId"
Write-Host "  Identifier URI: api://$appId"
Write-Host "  Issuer: https://sts.windows.net/$tenantId/"
Write-Host "  Audiences: api://$appId, $appId"
