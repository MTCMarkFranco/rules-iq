<#
.SYNOPSIS
    Create the Azure AI Search data source, skillset, and indexer for Rules-IQ.
#>

param(
    [string]$ResourceGroup = "RG-OpenAI",
    [string]$SearchService = "ai-search-hub-canada"
)

$ErrorActionPreference = "Stop"

$searchEndpoint = "https://$SearchService.search.windows.net"
$apiVersion = "2024-07-01"
$token = az account get-access-token --resource "https://search.azure.com" --query accessToken -o tsv

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type"  = "application/json"
}

# Create Data Source
Write-Host "Creating data source: ds-policy-documents" -ForegroundColor Yellow
$dsDef = Get-Content "$PSScriptRoot\..\definitions\datasource.json" -Raw
Invoke-RestMethod `
    -Uri "$searchEndpoint/datasources/ds-policy-documents?api-version=$apiVersion" `
    -Method PUT `
    -Headers $headers `
    -Body $dsDef `
    -ContentType "application/json"
Write-Host "Data source created." -ForegroundColor Green

# Create Skillset
Write-Host "Creating skillset: ss-rule-extraction" -ForegroundColor Yellow
$ssDef = Get-Content "$PSScriptRoot\..\definitions\skillset.json" -Raw
Invoke-RestMethod `
    -Uri "$searchEndpoint/skillsets/ss-rule-extraction?api-version=$apiVersion" `
    -Method PUT `
    -Headers $headers `
    -Body $ssDef `
    -ContentType "application/json"
Write-Host "Skillset created." -ForegroundColor Green

# Create Indexer
Write-Host "Creating indexer: ixr-policy-rules" -ForegroundColor Yellow
$ixrDef = Get-Content "$PSScriptRoot\..\definitions\indexer.json" -Raw
Invoke-RestMethod `
    -Uri "$searchEndpoint/indexers/ixr-policy-rules?api-version=$apiVersion" `
    -Method PUT `
    -Headers $headers `
    -Body $ixrDef `
    -ContentType "application/json"
Write-Host "Indexer created." -ForegroundColor Green

Write-Host "`nAll data-plane objects created successfully." -ForegroundColor Green
