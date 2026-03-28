@description('Base name for App Configuration resources')
param baseName string

@description('Azure region')
param location string

@description('The safe schema JSON content')
param safeSchemaJson string

var configStoreName = '${baseName}-appconfig'

resource appConfig 'Microsoft.AppConfiguration/configurationStores@2023-03-01' = {
  name: configStoreName
  location: location
  sku: { name: 'free' }
  properties: {
    disableLocalAuth: false
    enablePurgeProtection: false
  }
}

// Store the safe schema as a configuration value
resource safeSchemaEntry 'Microsoft.AppConfiguration/configurationStores/keyValues@2023-03-01' = {
  parent: appConfig
  name: 'SafeSchema'
  properties: {
    value: safeSchemaJson
  }
}

@description('The endpoint of the App Configuration store')
output endpoint string = appConfig.properties.endpoint

@description('The name of the App Configuration store')
output configStoreName string = appConfig.name
