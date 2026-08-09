// main.bicep — Orchestrates all MealPlanner Azure infrastructure modules.
// Deploy with: az deployment group create -g <resource-group> -f main.bicep -p @environments/uat.bicepparam

targetScope = 'resourceGroup'

// ─── Parameters ────────────────────────────────────────────────────────────────

@description('Azure region for all resources.')
param location string = 'canadacentral'

@allowed(['uat', 'prd'])
@description('Environment name (uat or prd). Used in resource naming and tagging.')
param environmentName string

@description('API container image tag (e.g., 1.2.0).')
param apiImageTag string

@description('Web container image tag (e.g., 1.2.0).')
param webImageTag string

@secure()
@description('JWT HMAC signing key shared between API and Web.')
param jwtSigningKey string

@description('Google OAuth client ID.')
param googleClientId string

@secure()
@description('Google OAuth client secret.')
param googleClientSecret string

@description('Name of the person or tool creating the deployment.')
param createdBy string

@description('Date the deployment was created (YYYY-MM-DD). Defaults to today.')
param createdDate string = utcNow('yyyy-MM-dd')

// ─── Variables ─────────────────────────────────────────────────────────────────

var baseTags = {
  app: 'mealplanner'
  environment: environmentName
  region: location
  managedBy: 'bicep'
  repo: 'cam96/MealPlanner'
  createdBy: createdBy
  createdDate: createdDate
}

// ─── Modules ───────────────────────────────────────────────────────────────────

module networking 'modules/networking.bicep' = {
  name: 'networking'
  params: {
    location: location
    tags: union(baseTags, { purpose: 'Network isolation for container apps' })
    environmentName: environmentName
  }
}

module containerRegistry 'modules/container-registry.bicep' = {
  name: 'container-registry'
  params: {
    location: location
    tags: union(baseTags, { purpose: 'Stores Docker images for API and Web services' })
  }
}

module storage 'modules/storage.bicep' = {
  name: 'storage'
  params: {
    location: location
    tags: union(baseTags, { purpose: 'Persistent storage for SQLite database and backups' })
    environmentName: environmentName
  }
}

module logAnalytics 'modules/log-analytics.bicep' = {
  name: 'log-analytics'
  params: {
    location: location
    tags: union(baseTags, { purpose: 'Centralized logging and monitoring for container apps' })
    environmentName: environmentName
  }
}

module containerAppsEnvironment 'modules/container-apps-environment.bicep' = {
  name: 'container-apps-environment'
  params: {
    location: location
    tags: union(baseTags, { purpose: 'Hosts API and Web container apps' })
    environmentName: environmentName
    subnetId: networking.outputs.subnetId
    logAnalyticsCustomerId: logAnalytics.outputs.workspaceCustomerId
    logAnalyticsSharedKey: logAnalytics.outputs.workspaceSharedKey
    storageAccountName: storage.outputs.storageAccountName
    storageAccountKey: storage.outputs.storageAccountKey
    dataShareName: storage.outputs.dataShareName
    backupsShareName: storage.outputs.backupsShareName
  }
}

module apiApp 'modules/container-app-api.bicep' = {
  name: 'container-app-api'
  params: {
    location: location
    tags: union(baseTags, { purpose: 'Backend API serving meal planning data' })
    environmentName: environmentName
    containerAppsEnvironmentId: containerAppsEnvironment.outputs.environmentId
    registryLoginServer: containerRegistry.outputs.loginServer
    registryName: containerRegistry.outputs.registryName
    imageTag: apiImageTag
    jwtSigningKey: jwtSigningKey
  }
}

module webApp 'modules/container-app-web.bicep' = {
  name: 'container-app-web'
  params: {
    location: location
    tags: union(baseTags, { purpose: 'Blazor UI for meal planning' })
    environmentName: environmentName
    containerAppsEnvironmentId: containerAppsEnvironment.outputs.environmentId
    registryLoginServer: containerRegistry.outputs.loginServer
    registryName: containerRegistry.outputs.registryName
    imageTag: webImageTag
    apiInternalFqdn: apiApp.outputs.fqdn
    jwtSigningKey: jwtSigningKey
    googleClientId: googleClientId
    googleClientSecret: googleClientSecret
  }
}

module keyVault 'modules/key-vault.bicep' = {
  name: 'key-vault'
  params: {
    location: location
    tags: union(baseTags, { purpose: 'Stores secrets for JWT signing and OAuth credentials' })
    environmentName: environmentName
    containerAppsPrincipalId: apiApp.outputs.principalId
    jwtSigningKey: jwtSigningKey
    googleClientId: googleClientId
    googleClientSecret: googleClientSecret
  }
}

// ─── Outputs ───────────────────────────────────────────────────────────────────

@description('The external FQDN of the Web Container App (point Cloudflare DNS here).')
output webFqdn string = webApp.outputs.fqdn

@description('The internal FQDN of the API Container App.')
output apiFqdn string = apiApp.outputs.fqdn

@description('The login server of the container registry.')
output registryLoginServer string = containerRegistry.outputs.loginServer

@description('The default domain of the Container Apps Environment.')
output environmentDefaultDomain string = containerAppsEnvironment.outputs.defaultDomain
