// main.bicep
// NOTE: Initial infrastructure was provisioned manually through the Azure Portal
// during early development. This file documents the resulting architecture as
// Infrastructure as Code and is the source of truth for future re-deployments.

param location string = 'centralus'
param sqlAdminLogin string = 'animeadmin'

@secure()
param sqlAdminPassword string

resource sqlServer 'Microsoft.Sql/servers@2022-05-01-preview' = {
  name: 'anime-tracker-server'
  location: location
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2022-05-01-preview' = {
  parent: sqlServer
  name: 'anime-tracker-db'
  location: location
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
}

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: 'animetrackerregistry'
  location: location
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: true
  }
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: 'animetrackerstore'
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
}

resource containerAppsEnv 'Microsoft.App/managedEnvironments@2023-05-01' = {
  name: 'managedEnvironment-mediatrackerrg'
  location: location
}