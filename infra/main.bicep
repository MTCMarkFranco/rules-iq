targetScope = 'subscription'

@description('Location for all resources')
param location string = 'eastus'

@description('Name of the main resource group')
param resourceGroupName string = 'rg-rules-iq'

@description('Name of the existing OpenAI resource group')
param openAIResourceGroupName string = 'RG-OpenAI'

@description('Name of the existing AI Search service')
param searchServiceName string = 'ai-search-hub-canada'

@description('Name of the existing storage account for policy docs')
param policyStorageAccountName string = 'sadatafileshubcanada'

@description('Name of the existing OpenAI resource')
param openAIAccountName string = 'rg-openai-hub'

// Phase 1: Resource Group
module resourceGroup 'modules/resource-group.bicep' = {
  name: 'deploy-resource-group'
  params: {
    name: resourceGroupName
    location: location
  }
}

// Phase 1: User-Assigned Managed Identity
module managedIdentity 'modules/managed-identity.bicep' = {
  name: 'deploy-managed-identity'
  scope: az.resourceGroup(resourceGroupName)
  params: {
    name: 'id-rulesiq'
    location: location
  }
  dependsOn: [resourceGroup]
}

// Phase 1: Storage for AI Foundry Hub
module hubStorage 'modules/storage.bicep' = {
  name: 'deploy-hub-storage'
  scope: az.resourceGroup(resourceGroupName)
  params: {
    name: 'sarulesiqhub'
    location: location
  }
  dependsOn: [resourceGroup]
}

// Phase 3: App Service (Indexer Skill Host)
module appService 'modules/app-service.bicep' = {
  name: 'deploy-app-service'
  scope: az.resourceGroup(resourceGroupName)
  params: {
    appName: 'app-rulesiq-indexer-skill'
    planName: 'asp-rulesiq'
    location: location
  }
  dependsOn: [resourceGroup]
}

// Phase 3: AI Foundry Hub
module aiFoundryHub 'modules/ai-foundry-hub.bicep' = {
  name: 'deploy-ai-foundry-hub'
  scope: az.resourceGroup(resourceGroupName)
  params: {
    hubName: 'rulesiq-ai-hub'
    location: location
    storageAccountId: hubStorage.outputs.storageAccountId
    managedIdentityId: managedIdentity.outputs.identityId
    openAIAccountName: openAIAccountName
    openAIResourceGroupName: openAIResourceGroupName
    searchServiceName: searchServiceName
    searchResourceGroupName: openAIResourceGroupName
  }
}

// Phase 3: AI Foundry Project
module aiFoundryProject 'modules/ai-foundry-project.bicep' = {
  name: 'deploy-ai-foundry-project'
  scope: az.resourceGroup(resourceGroupName)
  params: {
    projectName: 'rulesiq-agent-project'
    hubId: aiFoundryHub.outputs.hubId
    location: location
  }
}

// Phase 4: RBAC Assignments (AI Search system MI must already be enabled
// by configure-search.ps1 before this deployment runs)
module rbac 'modules/rbac-assignments.bicep' = {
  name: 'deploy-rbac'
  scope: az.resourceGroup(openAIResourceGroupName)
  params: {
    searchServiceName: searchServiceName
    openAIAccountName: openAIAccountName
    policyStorageAccountName: policyStorageAccountName
    appServicePrincipalId: appService.outputs.principalId
    managedIdentityPrincipalId: managedIdentity.outputs.principalId
  }
  dependsOn: [aiFoundryHub]
}

output resourceGroupName string = resourceGroupName
output managedIdentityId string = managedIdentity.outputs.identityId
output appServiceHostname string = appService.outputs.hostname
output hubId string = aiFoundryHub.outputs.hubId
output projectId string = aiFoundryProject.outputs.projectId
