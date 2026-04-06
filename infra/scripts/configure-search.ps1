<#
.SYNOPSIS
    Configure existing Azure AI Search and Storage services for Rules-IQ.
#>

param(
    [string]$ResourceGroup = "RG-OpenAI",
    [string]$SearchService = "ai-search-hub-canada",
    [string]$StorageAccount = "sadatafileshubcanada"
)

$ErrorActionPreference = "Stop"

Write-Host "Configuring AI Search: $SearchService" -ForegroundColor Yellow

# Enable Semantic Ranker
Write-Host "  Enabling semantic ranker..."
az search service update `
    --name $SearchService `
    --resource-group $ResourceGroup `
    --semantic-search free `
    --output none 2>$null

# Disable local auth on AI Search
Write-Host "  Disabling local auth on AI Search..."
az search service update `
    --name $SearchService `
    --resource-group $ResourceGroup `
    --disable-local-auth true `
    --auth-options aadOrApiKey `
    --output none 2>$null

# Enable system-assigned managed identity on AI Search
Write-Host "  Enabling system-assigned managed identity..."
az search service update `
    --name $SearchService `
    --resource-group $ResourceGroup `
    --identity-type SystemAssigned `
    --output none 2>$null

Write-Host "Configuring Storage: $StorageAccount" -ForegroundColor Yellow

# Disable shared key access
Write-Host "  Disabling shared key access..."
az storage account update `
    --name $StorageAccount `
    --resource-group $ResourceGroup `
    --allow-shared-key-access false `
    --output none 2>$null

# Create policy-documents blob container
Write-Host "  Creating policy-documents container..."
az storage container create `
    --name policy-documents `
    --account-name $StorageAccount `
    --auth-mode login `
    --output none 2>$null

Write-Host "Configuration complete." -ForegroundColor Green
