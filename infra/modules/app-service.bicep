param appName string
param planName string
param location string

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: planName
  location: location
  sku: {
    name: 'B1'
    tier: 'Basic'
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

resource appService 'Microsoft.Web/sites@2023-12-01' = {
  name: appName
  location: location
  kind: 'app,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      alwaysOn: true
      minTlsVersion: '1.2'
      appSettings: [
        {
          name: 'AzureOpenAI__Endpoint'
          value: 'https://rg-openai-hub.cognitiveservices.azure.com/'
        }
        {
          name: 'AzureOpenAI__DeploymentName'
          value: 'gpt-4.1'
        }
        {
          name: 'AzureOpenAI__EmbeddingDeploymentName'
          value: 'text-embedding-3-large'
        }
        {
          name: 'AzureSearch__Endpoint'
          value: 'https://ai-search-hub-canada.search.windows.net'
        }
        {
          name: 'AzureSearch__IndexName'
          value: 'idx-rules-iq'
        }
      ]
    }
  }
}

output hostname string = appService.properties.defaultHostName
output principalId string = appService.identity.principalId
output appServiceId string = appService.id
