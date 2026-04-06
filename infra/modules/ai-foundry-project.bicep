param projectName string
param hubId string
param location string

resource project 'Microsoft.MachineLearningServices/workspaces@2024-04-01' = {
  name: projectName
  location: location
  kind: 'Project'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    friendlyName: 'Rules-IQ Agent Project'
    description: 'AI Foundry Project for rule extraction, normalization, and evaluation agents'
    hubResourceId: hubId
  }
}

output projectId string = project.id
output projectName string = project.name
