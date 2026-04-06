param searchServiceName string
param openAIAccountName string
param policyStorageAccountName string
param appServicePrincipalId string
param managedIdentityPrincipalId string

// Built-in role definition IDs
var cognitiveServicesOpenAIUser = '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd'
var storageBlobDataReader = '2a2b9908-6ea1-4ae2-8e65-a410df84e7d1'
var searchIndexDataReader = '1407120a-92aa-4202-b7e9-c0e197c71c8f'
var searchIndexDataContributor = '8ebe5a00-799e-43f5-93ac-243d3dce84a7'

// Existing resources
resource searchService 'Microsoft.Search/searchServices@2024-03-01-preview' existing = {
  name: searchServiceName
}

resource openAIAccount 'Microsoft.CognitiveServices/accounts@2024-04-01-preview' existing = {
  name: openAIAccountName
}

resource policyStorage 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: policyStorageAccountName
}

// #1: AI Search → OpenAI (embeddings via integrated vectorizer)
resource searchToOpenAI 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(searchService.id, openAIAccount.id, cognitiveServicesOpenAIUser)
  scope: openAIAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', cognitiveServicesOpenAIUser)
    principalId: searchService.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// #2: AI Search → Blob Storage (read PDFs)
resource searchToBlob 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(searchService.id, policyStorage.id, storageBlobDataReader)
  scope: policyStorage
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageBlobDataReader)
    principalId: searchService.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// #3: App Service → OpenAI (rule extraction calls)
resource appToOpenAI 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid('app-service', openAIAccount.id, cognitiveServicesOpenAIUser)
  scope: openAIAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', cognitiveServicesOpenAIUser)
    principalId: appServicePrincipalId
    principalType: 'ServicePrincipal'
  }
}

// #4: App Service → AI Search (query index for context)
resource appToSearch 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid('app-service', searchService.id, searchIndexDataReader)
  scope: searchService
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', searchIndexDataReader)
    principalId: appServicePrincipalId
    principalType: 'ServicePrincipal'
  }
}

// #5: Managed Identity → OpenAI (AI Foundry agents)
resource identityToOpenAI 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid('id-rulesiq', openAIAccount.id, cognitiveServicesOpenAIUser)
  scope: openAIAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', cognitiveServicesOpenAIUser)
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// #6: Managed Identity → AI Search (read rules index)
resource identityToSearchRead 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid('id-rulesiq', searchService.id, searchIndexDataReader)
  scope: searchService
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', searchIndexDataReader)
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// #7: Managed Identity → AI Search (update index for versioned rules)
resource identityToSearchWrite 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid('id-rulesiq', searchService.id, searchIndexDataContributor)
  scope: searchService
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', searchIndexDataContributor)
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// #8: Managed Identity → Blob Storage (read source documents)
resource identityToBlob 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid('id-rulesiq', policyStorage.id, storageBlobDataReader)
  scope: policyStorage
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageBlobDataReader)
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}
