// Module: storage.bicep
// Creates a Storage Account with Azure Files shares for SQLite database and backups.

@description('Azure region for the resources.')
param location string

@description('Tags to apply to all resources.')
param tags object

@description('Environment name used in resource naming (uat or prd).')
param environmentName string

var workload = 'mealplanner'
var storageAccountName = 'st${workload}${environmentName}'

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  tags: tags
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    allowBlobPublicAccess: false
  }
}

resource fileServices 'Microsoft.Storage/storageAccounts/fileServices@2023-05-01' = {
  parent: storageAccount
  name: 'default'
}

resource dataShare 'Microsoft.Storage/storageAccounts/fileServices/shares@2023-05-01' = {
  parent: fileServices
  name: 'data'
  properties: {
    shareQuota: 1 // 1 GB — sufficient for SQLite + CNF dataset
    accessTier: 'TransactionOptimized'
  }
}

resource backupsShare 'Microsoft.Storage/storageAccounts/fileServices/shares@2023-05-01' = {
  parent: fileServices
  name: 'backups'
  properties: {
    shareQuota: 1 // 1 GB — rotating pre-migration backups
    accessTier: 'TransactionOptimized'
  }
}

@description('The resource ID of the storage account.')
output storageAccountId string = storageAccount.id

@description('The name of the storage account.')
output storageAccountName string = storageAccount.name

@description('The access key for the storage account.')
#disable-next-line outputs-should-not-contain-secrets
output storageAccountKey string = storageAccount.listKeys().keys[0].value

@description('The name of the data file share.')
output dataShareName string = dataShare.name

@description('The name of the backups file share.')
output backupsShareName string = backupsShare.name
