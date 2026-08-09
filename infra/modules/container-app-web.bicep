// Module: container-app-web.bicep
// Creates the Web Container App with external ingress, Cloudflare IP restrictions, and Key Vault secrets.

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

@description('The internal FQDN of the API Container App for service discovery.')
param apiInternalFqdn string

@secure()
@description('JWT HMAC signing key.')
param jwtSigningKey string

@secure()
@description('Google OAuth client ID.')
param googleClientId string

@secure()
@description('Google OAuth client secret.')
param googleClientSecret string

var workload = 'mealplanner'
var appName = 'ca-${workload}-web-${environmentName}'
var imageName = '${registryLoginServer}/mealplanner-web:${imageTag}'

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-11-01-preview' existing = {
  name: registryName
}

resource webApp 'Microsoft.App/containerApps@2024-03-01' = {
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
        external: true
        targetPort: 8080
        transport: 'http'
        // Restrict inbound traffic to Cloudflare IP ranges for origin protection.
        // See: https://www.cloudflare.com/ips/
        ipSecurityRestrictions: [
          {
            name: 'allow-cloudflare-ipv4-1'
            ipAddressRange: '173.245.48.0/20'
            action: 'Allow'
          }
          {
            name: 'allow-cloudflare-ipv4-2'
            ipAddressRange: '103.21.244.0/22'
            action: 'Allow'
          }
          {
            name: 'allow-cloudflare-ipv4-3'
            ipAddressRange: '103.22.200.0/22'
            action: 'Allow'
          }
          {
            name: 'allow-cloudflare-ipv4-4'
            ipAddressRange: '103.31.4.0/22'
            action: 'Allow'
          }
          {
            name: 'allow-cloudflare-ipv4-5'
            ipAddressRange: '141.101.64.0/18'
            action: 'Allow'
          }
          {
            name: 'allow-cloudflare-ipv4-6'
            ipAddressRange: '108.162.192.0/18'
            action: 'Allow'
          }
          {
            name: 'allow-cloudflare-ipv4-7'
            ipAddressRange: '190.93.240.0/20'
            action: 'Allow'
          }
          {
            name: 'allow-cloudflare-ipv4-8'
            ipAddressRange: '188.114.96.0/20'
            action: 'Allow'
          }
          {
            name: 'allow-cloudflare-ipv4-9'
            ipAddressRange: '197.234.240.0/22'
            action: 'Allow'
          }
          {
            name: 'allow-cloudflare-ipv4-10'
            ipAddressRange: '198.41.128.0/17'
            action: 'Allow'
          }
          {
            name: 'allow-cloudflare-ipv4-11'
            ipAddressRange: '162.158.0.0/15'
            action: 'Allow'
          }
          {
            name: 'allow-cloudflare-ipv4-12'
            ipAddressRange: '104.16.0.0/13'
            action: 'Allow'
          }
          {
            name: 'allow-cloudflare-ipv4-13'
            ipAddressRange: '104.24.0.0/14'
            action: 'Allow'
          }
          {
            name: 'allow-cloudflare-ipv4-14'
            ipAddressRange: '172.64.0.0/13'
            action: 'Allow'
          }
          {
            name: 'allow-cloudflare-ipv4-15'
            ipAddressRange: '131.0.72.0/22'
            action: 'Allow'
          }
          {
            name: 'deny-all'
            ipAddressRange: '0.0.0.0/0'
            action: 'Deny'
          }
        ]
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
        {
          name: 'google-client-id'
          value: googleClientId
        }
        {
          name: 'google-client-secret'
          value: googleClientSecret
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'web'
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
              name: 'services__api__http__0'
              value: 'http://${apiInternalFqdn}'
            }
            {
              name: 'Authentication__Jwt__Key'
              secretRef: 'jwt-signing-key'
            }
            {
              name: 'Authentication__Google__ClientId'
              secretRef: 'google-client-id'
            }
            {
              name: 'Authentication__Google__ClientSecret'
              secretRef: 'google-client-secret'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 0
        maxReplicas: 2
      }
    }
  }
}

// Grant AcrPull role to the Container App's managed identity
resource acrPullRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(containerRegistry.id, webApp.id, '7f951dda-4ed3-4680-a7ca-43fe172d538d')
  scope: containerRegistry
  properties: {
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      '7f951dda-4ed3-4680-a7ca-43fe172d538d' // AcrPull
    )
    principalId: webApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

@description('The resource ID of the Web Container App.')
output appId string = webApp.id

@description('The name of the Web Container App.')
output appName string = webApp.name

@description('The FQDN of the Web Container App (external).')
output fqdn string = webApp.properties.configuration.ingress.fqdn

@description('The principal ID of the Web Container App managed identity.')
output principalId string = webApp.identity.principalId
