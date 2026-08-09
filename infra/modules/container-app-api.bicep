// Module: container-app-api.bicep
// Creates the API Container App with internal ingress, volume mounts, and Key Vault secrets.

@description('Azure region for the resources.')
param location string

@description('Tags to apply to all resources.')
param tags object

@description('Environment name used in resource naming (uat or prd).')
param environmentName string

@description('The resource ID of the Container Apps Environment.')
param containerAppsEnvironmentId string

@description('The login server of the container registry.')
param registryLoginServer string

@description('The name of the container registry.')
param registryName string

@description('The image tag to deploy (e.g., 1.2.0).')
param imageTag string

@secure()
@description('JWT HMAC signing key.')
param jwtSigningKey string

var workload = 'mealplanner'
var appName = 'ca-${workload}-api-${environmentName}'
var imageName = '${registryLoginServer}/mealplanner-api:${imageTag}'

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-11-01-preview' existing = {
  name: registryName
}

resource apiApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: appName
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: containerAppsEnvironmentId
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: false
        targetPort: 8080
        transport: 'http'
      }
      registries: [
        {
          server: registryLoginServer
          identity: 'system'
        }
      ]
      secrets: [
        {
          name: 'jwt-signing-key'
          value: jwtSigningKey
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'api'
          image: imageName
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
            {
              name: 'ASPNETCORE_URLS'
              value: 'http://+:8080'
            }
            {
              name: 'ConnectionStrings__mealplanner'
              value: 'Data Source=/data/mealplanner.db'
            }
            {
              name: 'MealPlanner__BackupDirectory'
              value: '/backups'
            }
            {
              name: 'MealPlanner__CnfDirectory'
              value: '/data/cnf'
            }
            {
              name: 'MealPlanner__SeedDemoData'
              value: 'false'
            }
            {
              name: 'Authentication__Jwt__Key'
              secretRef: 'jwt-signing-key'
            }
          ]
          volumeMounts: [
            {
              volumeName: 'data'
              mountPath: '/data'
            }
            {
              volumeName: 'backups'
              mountPath: '/backups'
            }
          ]
          probes: [
            {
              type: 'Liveness'
              httpGet: {
                path: '/ping'
                port: 8080
              }
              initialDelaySeconds: 10
              periodSeconds: 30
            }
            {
              type: 'Readiness'
              httpGet: {
                path: '/ping'
                port: 8080
              }
              initialDelaySeconds: 5
              periodSeconds: 10
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 1 // SQLite is single-writer; must not scale beyond 1
      }
      volumes: [
        {
          name: 'data'
          storageName: 'data'
          storageType: 'AzureFile'
        }
        {
          name: 'backups'
          storageName: 'backups'
          storageType: 'AzureFile'
        }
      ]
    }
  }
}

// Grant AcrPull role to the Container App's managed identity
resource acrPullRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(containerRegistry.id, apiApp.id, '7f951dda-4ed3-4680-a7ca-43fe172d538d')
  scope: containerRegistry
  properties: {
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      '7f951dda-4ed3-4680-a7ca-43fe172d538d' // AcrPull
    )
    principalId: apiApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

@description('The resource ID of the API Container App.')
output appId string = apiApp.id

@description('The name of the API Container App.')
output appName string = apiApp.name

@description('The FQDN of the API Container App (internal).')
output fqdn string = apiApp.properties.configuration.ingress.fqdn

@description('The principal ID of the API Container App managed identity.')
output principalId string = apiApp.identity.principalId
