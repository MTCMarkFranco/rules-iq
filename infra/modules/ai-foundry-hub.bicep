param hubName string
param location string
param storageAccountId string
param managedIdentityId string
param openAIAccountName string
param openAIResourceGroupName string
param searchServiceName string
param searchResourceGroupName string

resource aiHub 'Microsoft.MachineLearningServices/workspaces@2024-04-01' = {
  name: hubName
  location: location
  kind: 'Hub'
  identity: {
    type: 'SystemAssigned,UserAssigned'
    userAssignedIdentities: {
      '${managedIdentityId}': {}
    }
  }
  properties: {
    friendlyName: 'Rules-IQ AI Hub'
    description: 'AI Foundry Hub for Rules-IQ agentic pipeline'
    storageAccount: storageAccountId
    primaryUserAssignedIdentity: managedIdentityId
  }
}

resource openAIConnection 'Microsoft.MachineLearningServices/workspaces/connections@2024-04-01' = {
  parent: aiHub
  name: 'openai-connection'
  properties: {
    category: 'AzureOpenAI'
    target: 'https://${openAIAccountName}.cognitiveservices.azure.com/'
    authType: 'AAD'
    metadata: {
      ApiType: 'Azure'
      ResourceId: '/subscriptions/${subscription().subscriptionId}/resourceGroups/${openAIResourceGroupName}/providers/Microsoft.CognitiveServices/accounts/${openAIAccountName}'
    }
  }
}

resource searchConnection 'Microsoft.MachineLearningServices/workspaces/connections@2024-04-01' = {
  parent: aiHub
  name: 'search-connection'
  properties: {
    category: 'CognitiveSearch'
    target: 'https://${searchServiceName}.search.windows.net'
    authType: 'AAD'
    metadata: {
      ResourceId: '/subscriptions/${subscription().subscriptionId}/resourceGroups/${searchResourceGroupName}/providers/Microsoft.Search/searchServices/${searchServiceName}'
    }
  }
}

output hubId string = aiHub.id
output hubName string = aiHub.name
