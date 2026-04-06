<#
.SYNOPSIS
    Create AI Foundry agents via the v2 REST API for Rules-IQ.
#>

param(
    [string]$ResourceGroup = "rg-rules-iq",
    [string]$ProjectName = "rulesiq-agent-project",
    [string]$SubscriptionId = "28d10200-70b0-476c-b004-c6ae29265897"
)

$ErrorActionPreference = "Stop"

$token = az account get-access-token --resource "https://management.azure.com" --query accessToken -o tsv

# Get the project endpoint
$project = az ml workspace show `
    --name $ProjectName `
    --resource-group $ResourceGroup `
    --query "discovery_url" -o tsv 2>$null

# Fallback to constructing the endpoint
$agentEndpoint = "https://eastus.api.azureml.ms/agents/v1.0/subscriptions/$SubscriptionId/resourceGroups/$ResourceGroup/providers/Microsoft.MachineLearningServices/workspaces/$ProjectName"

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type"  = "application/json"
}

$agentFiles = Get-ChildItem "$PSScriptRoot\..\definitions\agents\*.json"

foreach ($file in $agentFiles) {
    $agentDef = Get-Content $file.FullName -Raw
    $agentName = ($agentDef | ConvertFrom-Json).name

    Write-Host "Creating agent: $agentName" -ForegroundColor Yellow

    try {
        Invoke-RestMethod `
            -Uri "$agentEndpoint/assistants?api-version=2024-12-01-preview" `
            -Method POST `
            -Headers $headers `
            -Body $agentDef `
            -ContentType "application/json"

        Write-Host "Agent '$agentName' created." -ForegroundColor Green
    }
    catch {
        Write-Warning "Failed to create agent '$agentName': $_"
    }
}

Write-Host "`nAll agents created." -ForegroundColor Green
