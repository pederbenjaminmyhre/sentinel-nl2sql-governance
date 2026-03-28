@description('Base name for Key Vault resources')
param baseName string

@description('Azure region')
param location string

@description('Tenant ID')
param tenantId string

var vaultName = '${baseName}-kv'

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: take(vaultName, 24)
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: tenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    enabledForDeployment: false
    enabledForDiskEncryption: false
    enabledForTemplateDeployment: false
  }
}

@description('The URI of the Key Vault')
output vaultUri string = keyVault.properties.vaultUri

@description('The name of the Key Vault')
output vaultName string = keyVault.name

@description('The resource ID of the Key Vault')
output vaultId string = keyVault.id
