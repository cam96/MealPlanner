// Module: container-apps-environment.bicep
// Creates a Container Apps Environment linked to the VNet subnet with Azure Files storage mounts.

@description('Azure region for the resources.')
param location string

@description('Tags to apply to all resources.')
param tags object

@description('Environment name used in resource naming (uat or prd).')
param environmentName string

@description('The resource ID of the subnet to deploy the Container Apps Environment into.')
param subnetId string

@description('The Log Analytics workspace customer ID.')
param logAnalyticsCustomerId string

@secure()
@description('The Log Analytics workspace shared key.')
param logAnalyticsSharedKey string

@description('The name of the storage account for Azure Files mounts.')
param storageAccountName string

@secure()
@description('The access key for the storage account.')
param storageAccountKey string

@description('The name of the data file share.')
param dataShareName string

@description('The name of the backups file share.')
param backupsShareName string

var workload = 'mealplanner'
var regionAbbreviation = 'cc'
var environmentResourceName = 'cae-${workload}-${environmentName}-${regionAbbreviation}'

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: environmentResourceName
  location: location
  tags: tags
  properties: {
    vnetConfiguration: {
      infrastructureSubnetId: subnetId
      internal: false
    }
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalyticsCustomerId
        sharedKey: logAnalyticsSharedKey
      }
    }
  }
}

resource dataStorage 'Microsoft.App/managedEnvironments/storages@2024-03-01' = {
  parent: containerAppsEnvironment
  name: 'data'
  properties: {
    azureFile: {
      accountName: storageAccountName
      accountKey: storageAccountKey
      shareName: dataShareName
      accessMode: 'ReadWrite'
    }
  }
}

resource backupsStorage 'Microsoft.App/managedEnvironments/storages@2024-03-01' = {
  parent: containerAppsEnvironment
  name: 'backups'
  properties: {
    azureFile: {
      accountName: storageAccountName
      accountKey: storageAccountKey
      shareName: backupsShareName
      accessMode: 'ReadWrite'
    }
  }
}

@description('The resource ID of the Container Apps Environment.')
output environmentId string = containerAppsEnvironment.id

@description('The name of the Container Apps Environment.')
output environmentName string = containerAppsEnvironment.name

@description('The default domain of the Container Apps Environment.')
output defaultDomain string = containerAppsEnvironment.properties.defaultDomain

@description('The static IP address of the Container Apps Environment.')
output staticIp string = containerAppsEnvironment.properties.staticIp
