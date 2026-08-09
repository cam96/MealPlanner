// Module: log-analytics.bicep
// Creates a Log Analytics workspace for Container Apps monitoring and diagnostics.

@description('Azure region for the resources.')
param location string

@description('Tags to apply to all resources.')
param tags object

@description('Environment name used in resource naming (uat or prd).')
param environmentName string

var workload = 'mealplanner'
var workspaceName = 'log-${workload}-${environmentName}'

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: workspaceName
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

@description('The resource ID of the Log Analytics workspace.')
output workspaceId string = logAnalyticsWorkspace.id

@description('The customer ID (workspace ID) of the Log Analytics workspace.')
output workspaceCustomerId string = logAnalyticsWorkspace.properties.customerId

@description('The shared key of the Log Analytics workspace.')
#disable-next-line outputs-should-not-contain-secrets
output workspaceSharedKey string = logAnalyticsWorkspace.listKeys().primarySharedKey

@description('The name of the Log Analytics workspace.')
output workspaceName string = logAnalyticsWorkspace.name
