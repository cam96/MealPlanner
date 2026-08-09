// Module: networking.bicep
// Creates a Virtual Network with a subnet delegated to Azure Container Apps Environment.

@description('Azure region for the resources.')
param location string

@description('Tags to apply to all resources.')
param tags object

@description('Environment name used in resource naming (uat or prd).')
param environmentName string

var workload = 'mealplanner'
var regionAbbreviation = 'cc' // canadacentral
var vnetName = 'vnet-${workload}-${environmentName}-${regionAbbreviation}'
var subnetName = 'snet-${workload}-${environmentName}-${regionAbbreviation}'

resource vnet 'Microsoft.Network/virtualNetworks@2024-05-01' = {
  name: vnetName
  location: location
  tags: tags
  properties: {
    addressSpace: {
      addressPrefixes: [
        '10.0.0.0/16'
      ]
    }
    subnets: [
      {
        name: subnetName
        properties: {
          addressPrefix: '10.0.0.0/23'
          delegations: [
            {
              name: 'Microsoft.App.environments'
              properties: {
                serviceName: 'Microsoft.App/environments'
              }
            }
          ]
        }
      }
    ]
  }
}

@description('The resource ID of the virtual network.')
output vnetId string = vnet.id

@description('The resource ID of the Container Apps subnet.')
output subnetId string = vnet.properties.subnets[0].id

@description('The name of the virtual network.')
output vnetName string = vnet.name

@description('The name of the subnet.')
output subnetName string = subnetName
