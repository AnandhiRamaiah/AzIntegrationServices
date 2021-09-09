param sbNamespaceName string
param sbQueueName string
param sbTopicName string

@allowed([
  'Basic'
  'Standard'
  'Premium'
])
param sbSku string = 'Standard'
param location string = resourceGroup().location

resource sbNamespace 'Microsoft.ServiceBus/namespaces@2018-01-01-preview' = {
  name: sbNamespaceName
  location: location
  sku: {
    name: sbSku
  }
  properties: {}
  
  resource sbQueue 'queues@2021-01-01-preview' = {
    name: sbQueueName
    properties: {
      lockDuration: 'PT2M'
      maxSizeInMegabytes: 2048
      requiresDuplicateDetection: true
      requiresSession: false    
      deadLetteringOnMessageExpiration: false
      duplicateDetectionHistoryTimeWindow: 'PT20M'
      maxDeliveryCount: 5        
      enablePartitioning: true        
    }
  
    dependsOn: [
      sbNamespace
    ]
  }

  resource sbTopic 'topics@2021-01-01-preview' = {
    name: sbTopicName
    properties: {
      defaultMessageTimeToLive: 'PT2M'
      maxSizeInMegabytes: 2048
      requiresDuplicateDetection: true      
      supportOrdering: true      
      enablePartitioning: true      
    }
    dependsOn: [
      sbNamespace
    ]
  }
}

output sbId string = sbNamespace.id
