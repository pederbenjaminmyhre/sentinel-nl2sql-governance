@description('Base name for APIM resources')
param baseName string

@description('Azure region')
param location string

@description('Publisher email for APIM')
param publisherEmail string

@description('Backend Function App hostname')
param functionAppHostName string

var apimName = '${baseName}-apim'

resource apim 'Microsoft.ApiManagement/service@2023-09-01-preview' = {
  name: apimName
  location: location
  sku: {
    name: 'Consumption'
    capacity: 0
  }
  properties: {
    publisherEmail: publisherEmail
    publisherName: 'Sentinel NL2SQL'
  }
}

// API definition
resource api 'Microsoft.ApiManagement/service/apis@2023-09-01-preview' = {
  parent: apim
  name: 'sentinel-api'
  properties: {
    displayName: 'Sentinel Query API'
    path: 'sentinel'
    protocols: [ 'https' ]
    serviceUrl: 'https://${functionAppHostName}/api'
    subscriptionRequired: true
  }
}

// Query operation
resource queryOperation 'Microsoft.ApiManagement/service/apis/operations@2023-09-01-preview' = {
  parent: api
  name: 'query'
  properties: {
    displayName: 'Submit Query'
    method: 'POST'
    urlTemplate: '/query'
  }
}

// Health operation
resource healthOperation 'Microsoft.ApiManagement/service/apis/operations@2023-09-01-preview' = {
  parent: api
  name: 'health'
  properties: {
    displayName: 'Health Check'
    method: 'GET'
    urlTemplate: '/health'
  }
}

// Rate limiting policy
resource apiPolicy 'Microsoft.ApiManagement/service/apis/policies@2023-09-01-preview' = {
  parent: api
  name: 'policy'
  properties: {
    format: 'xml'
    value: '''
      <policies>
        <inbound>
          <base />
          <rate-limit calls="60" renewal-period="60" />
          <cors allow-credentials="false">
            <allowed-origins><origin>*</origin></allowed-origins>
            <allowed-methods><method>GET</method><method>POST</method></allowed-methods>
            <allowed-headers><header>*</header></allowed-headers>
          </cors>
        </inbound>
        <backend><base /></backend>
        <outbound><base /></outbound>
        <on-error><base /></on-error>
      </policies>
    '''
  }
}

@description('The gateway URL of APIM')
output gatewayUrl string = apim.properties.gatewayUrl

@description('The name of the APIM instance')
output apimName string = apim.name
