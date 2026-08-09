// Module: key-vault.bicep
// Creates a Key Vault for storing application secrets (JWT key, OAuth credentials).

@description('Azure region for the resources.')
param location string

@description('Tags to apply to all resources.')
param tags object

@description('Environment name used in resource naming (uat or prd).')
param environmentName string

@description('The principal ID of the Container Apps managed identity to grant secret access.')
param containerAppsPrincipalId string

@secure()
@description('JWT HMAC signing key.')
param jwtSigningKey string

@description('Google OAuth client ID.')
param googleClientId string

@secure()
@description('Google OAuth client secret.')
param googleClientSecret string

var workload = 'mealplanner'
var vaultName = 'kv-${workload}-${environmentName}'

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: vaultName
  location: location
  tags: tags
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    enabledForDeployment: false
    enabledForDiskEncryption: false
    enabledForTemplateDeployment: false
  }
}

// Grant the Container Apps managed identity "Key Vault Secrets User" role
resource secretsUserRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, containerAppsPrincipalId, '4633458b-17de-408a-b874-0445c86b69e6')
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      '4633458b-17de-408a-b874-0445c86b69e6' // Key Vault Secrets User
    )
    principalId: containerAppsPrincipalId
    principalType: 'ServicePrincipal'
  }
}

resource jwtSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'jwt-signing-key'
  properties: {
    value: jwtSigningKey
  }
}

resource googleClientIdSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'google-client-id'
  properties: {
    value: googleClientId
  }
}

resource googleClientSecretSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'google-client-secret'
  properties: {
    value: googleClientSecret
  }
}

@description('The resource ID of the Key Vault.')
output vaultId string = keyVault.id

@description('The URI of the Key Vault.')
output vaultUri string = keyVault.properties.vaultUri

@description('The name of the Key Vault.')
output vaultName string = keyVault.name

@description('The URI of the JWT signing key secret.')
output jwtSecretUri string = jwtSecret.properties.secretUri

@description('The URI of the Google client ID secret.')
output googleClientIdSecretUri string = googleClientIdSecret.properties.secretUri

@description('The URI of the Google client secret secret.')
output googleClientSecretSecretUri string = googleClientSecretSecret.properties.secretUri
