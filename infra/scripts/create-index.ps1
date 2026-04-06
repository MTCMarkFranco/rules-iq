<#
.SYNOPSIS
    Create the Azure AI Search index for Rules-IQ.
#>

param(
    [string]$ResourceGroup = "RG-OpenAI",
    [string]$SearchService = "ai-search-hub-canada"
)

$ErrorActionPreference = "Stop"

$searchEndpoint = "https://$SearchService.search.windows.net"
$token = az account get-access-token --resource "https://search.azure.com" --query accessToken -o tsv

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type"  = "application/json"
    "api-version"   = "2024-07-01"
}

Write-Host "Creating index: idx-rules-iq" -ForegroundColor Yellow

$indexDef = Get-Content "$PSScriptRoot\..\definitions\index.json" -Raw

$response = Invoke-RestMethod `
    -Uri "$searchEndpoint/indexes/idx-rules-iq?api-version=2024-07-01" `
    -Method PUT `
    -Headers $headers `
    -Body $indexDef `
    -ContentType "application/json"

Write-Host "Index 'idx-rules-iq' created successfully." -ForegroundColor Green
