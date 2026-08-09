// Module: container-registry.bicep
// Creates a shared Azure Container Registry (ACR) for storing Docker images.
// This ACR is shared across environments (uat/prd) — images are tagged by version.

@description('Azure region for the resources.')
param location string

@description('Tags to apply to all resources.')
param tags object

var registryName = 'crmealplanner'

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-11-01-preview' = {
  name: registryName
  location: location
  tags: tags
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: false
    publicNetworkAccess: 'Enabled'
  }
}

@description('The resource ID of the container registry.')
output registryId string = containerRegistry.id

@description('The login server URL of the container registry.')
output loginServer string = containerRegistry.properties.loginServer

@description('The name of the container registry.')
output registryName string = containerRegistry.name
