@description('Base name for all resources (e.g., "sentinel")')
param baseName string

@description('Azure region for all resources')
param location string = resourceGroup().location

@description('SQL administrator login')
param sqlAdminLogin string

@secure()
@description('SQL administrator password')
param sqlAdminPassword string

@description('Publisher email for API Management')
param publisherEmail string

@description('Safe schema JSON for allow-list configuration')
param safeSchemaJson string = loadTextContent('../config/safe_schema.json')

// ─── Application Insights + Log Analytics ───
resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${baseName}-logs'
  location: location
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: 30
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${baseName}-insights'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
  }
}

// ─── Key Vault (deployed first, no dependency on Function App) ───
module keyVault 'modules/key-vault.bicep' = {
  name: 'deploy-key-vault'
  params: {
    baseName: baseName
    location: location
    tenantId: subscription().tenantId
  }
}

// ─── Function App (depends on Key Vault URI) ───
module functionApp 'modules/function-app.bicep' = {
  name: 'deploy-function-app'
  params: {
    baseName: baseName
    location: location
    appInsightsInstrumentationKey: appInsights.properties.InstrumentationKey
    appInsightsConnectionString: appInsights.properties.ConnectionString
    keyVaultUri: keyVault.outputs.vaultUri
  }
}

// ─── Key Vault Role Assignment (grants Function App identity access) ───
resource kvSecretUserRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, baseName, '4633458b-17de-408a-b874-0445c86b69e6')
  scope: resourceGroup()
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
    principalId: functionApp.outputs.principalId
    principalType: 'ServicePrincipal'
  }
}

// ─── SQL Database ───
module sql 'modules/sql-replica.bicep' = {
  name: 'deploy-sql'
  params: {
    baseName: baseName
    location: location
    sqlAdminLogin: sqlAdminLogin
    sqlAdminPassword: sqlAdminPassword
  }
}

// ─── API Management ───
module apim 'modules/api-management.bicep' = {
  name: 'deploy-apim'
  params: {
    baseName: baseName
    location: location
    publisherEmail: publisherEmail
    functionAppHostName: functionApp.outputs.defaultHostName
  }
}

// ─── App Configuration ───
module appConfig 'modules/app-configuration.bicep' = {
  name: 'deploy-app-config'
  params: {
    baseName: baseName
    location: location
    safeSchemaJson: safeSchemaJson
  }
}

// ─── Outputs ───
output functionAppName string = functionApp.outputs.functionAppName
output functionAppHostName string = functionApp.outputs.defaultHostName
output sqlServerFqdn string = sql.outputs.serverFqdn
output sqlDatabaseName string = sql.outputs.databaseName
output keyVaultName string = keyVault.outputs.vaultName
output apimGatewayUrl string = apim.outputs.gatewayUrl
output appConfigEndpoint string = appConfig.outputs.endpoint
output logAnalyticsWorkspace string = logAnalytics.name
