@description('Location for all resources.')
param location string = resourceGroup().location

@description('The location of the Azure OpenAI resource to deploy GPT4o to, only required when deployAzureOpenAI is set to true.')
param azureOpenAILocation string = 'eastus'

@description('A prefix to add to the start of all resource names. Note: A "unique" suffix will also be added')
param prefix string = 'alttext'

@description('The container image to run')
param containerAppImage string = 'ghcr.io/scottholden/alttextgenaidemo:release'

@description('When set to true an Azure OpenAI resource will be deployed, a GPT-4-Turbo deployment will be created, and RBAC configured. If this is set to')
param deployAzureOpenAI bool = true

@description('The endpoint of the AOAI resource to use, only required when deployAzureOpenAI is set to false')
param azureOpenAIEndpoint string = ''

@description('The deployment name the app should use, it needs to support vision, so GPT-4v or GPT-4-Turbo or GPT-4o, only required when deployAzureOpenAI is set to false')
param azureOpenAIDeploymentName string = ''

@description('Tags to apply to all deployed resources')
param tags object = {}

var uniqueNameFormat = '${prefix}-{0}-${uniqueString(resourceGroup().id, prefix)}'

resource openai 'Microsoft.CognitiveServices/accounts@2023-10-01-preview' = if (deployAzureOpenAI) {
  name: format(uniqueNameFormat, 'openai')
  location: azureOpenAILocation
  tags: tags
  kind: 'OpenAI'
  sku: {
    name: 'S0'
  }
  properties: {
    customSubDomainName: format(uniqueNameFormat, 'openai')
  }
  resource visionDeployment 'deployments@2023-10-01-preview' = {
    name: 'gpt-4o'
    sku: {
      name: 'GlobalStandard'
      capacity: 15
    }
    properties: {
      model: {
        format: 'OpenAI'
        name: 'gpt-4o'
        version: '2024-05-13'
      }
      versionUpgradeOption: 'NoAutoUpgrade'
    }
  }
}

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: format(uniqueNameFormat, 'logs')
  location: location
  tags: tags
  properties: {
    retentionInDays: 30
    sku: {
      name: 'PerGB2018'
    }
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02-preview' = {
  name: format(uniqueNameFormat, 'insights')
  location: location
  tags: tags
  kind: 'web'
  properties: { 
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
  }
}

resource containerAppEnv 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: format(uniqueNameFormat, 'containerapp')
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
  }
}

resource containerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: prefix
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: containerAppEnv.id
    configuration: {
      ingress: {
        external: true
        targetPort: 8080
        allowInsecure: false
        traffic: [
          {
            latestRevision: true
            weight: 100
          }
        ]
      }
    }
    template: {
      revisionSuffix: 'release'
      containers: [
        {
          name: 'wikiai'
          image: containerAppImage
          resources: {
            cpu: json('.25')
            memory: '.5Gi'
          }
          env: [
            {
              name: 'aoai__endpoint'
              value: deployAzureOpenAI ? openai.properties.endpoint : azureOpenAIEndpoint
            }
            {
              name: 'aoai__chatCompletionVisionDeployment'
              value: deployAzureOpenAI ? openai::visionDeployment.name : azureOpenAIDeploymentName
            }
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              value: appInsights.properties.ConnectionString
            }
          ]
        }
      ]
      scale: {
        minReplicas: 0
        maxReplicas: 1
        rules: [
          {
            name: 'http-requests'
            http: {
              metadata: {
                concurrentRequests: '50'
              }
            }
          }
        ]
      }
    }
  }
}

resource cogSvcOpenAIUserRole 'Microsoft.Authorization/roleDefinitions@2022-05-01-preview' existing = {
  name: '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd' // Cognitive Services OpenAI User
  scope: subscription()
}

resource containerAppRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (deployAzureOpenAI) {
  name: guid(cogSvcOpenAIUserRole.id, containerApp.id, openai.id)
  scope: openai
  properties: {
    principalId: containerApp.identity.principalId
    roleDefinitionId: cogSvcOpenAIUserRole.id
    principalType: 'ServicePrincipal'
  }
}
