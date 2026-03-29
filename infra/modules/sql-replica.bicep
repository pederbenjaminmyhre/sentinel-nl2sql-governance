@description('Base name for SQL resources')
param baseName string

@description('Azure region')
param location string

@description('SQL administrator login')
param sqlAdminLogin string

@secure()
@description('SQL administrator password')
param sqlAdminPassword string

var serverName = '${baseName}-sql'
var databaseName = '${baseName}-db'

// SQL Server
resource sqlServer 'Microsoft.Sql/servers@2023-05-01-preview' = {
  name: serverName
  location: location
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

// Allow Azure services to access (for Function App)
resource firewallRule 'Microsoft.Sql/servers/firewallRules@2023-05-01-preview' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// Basic tier ($4.99/month fixed — no per-hour compute or license fees)
resource database 'Microsoft.Sql/servers/databases@2023-05-01-preview' = {
  parent: sqlServer
  name: databaseName
  location: location
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 2147483648
  }
}

@description('The fully qualified domain name of the SQL server')
output serverFqdn string = sqlServer.properties.fullyQualifiedDomainName

@description('The name of the database')
output databaseName string = database.name

@description('The name of the SQL server')
output serverName string = sqlServer.name

@description('Connection string for read-only access')
output connectionString string = 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Initial Catalog=${databaseName};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
