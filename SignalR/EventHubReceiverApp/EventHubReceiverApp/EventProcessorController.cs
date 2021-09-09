using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.SignalR;
using Azure.Storage.Blobs;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;

namespace EventHubReceiverApp
{
    public class EventProcessorController
    {

        private static string kStorageConnectionString = "DefaultEndpointsProtocol=https;AccountName=armwkshstg;AccountKey=KMNK9qum/0mduaXx5Gvknr6J4RoXgRiFJCEBe053EC3nN8sJpD0ZV1oF4HkmwPeP/Ok3YaFyW4ktlHZrZNi+Og==;EndpointSuffix=core.windows.net";
        private static string kBlobContainerNameString = "azure-webjobs-eventhub";
        private static string kEventHubConnectionString = "Endpoint=sb://armwkshpehns.servicebus.windows.net/;SharedAccessKeyName=armocreh-sendandlisten-rule;SharedAccessKey=hpSN+2Wq//HDkB0MLBkTunwo1xvmFe+f+kckFLW7zyk=;EntityPath=armocreh";
        private static string kConsumerGroupNameString = "armocreh-consumer-group-1";    

        private BlobContainerClient _blobContainerClient;
        private EventProcessorClient _eventProcessorClient;
        
        private async Task PrepareProcessorAsync()
        {

            _eventProcessorClient.ProcessEventAsync += ProcessEventHanlderAsync;
            _eventProcessorClient.ProcessErrorAsync += ProcessErrorHandlerAsync;            

            await _eventProcessorClient.StartProcessingAsync();         

        }

        private async Task ProcessEventHanlderAsync(ProcessEventArgs args)
        {

            string partition = args.Partition.PartitionId;
            byte[] eventBody = args.Data.EventBody.ToArray();
            var eventBodyString = Encoding.UTF8.GetString(eventBody);

            var eventInfoString = $"Partition:{partition}\nEventInfo:{eventBodyString}";
            await SendEventInfoAsync(eventInfoString);

        }

        private async Task ProcessErrorHandlerAsync(ProcessErrorEventArgs args)
        {

            var errorInfoString = $"ErrorInfo:{args.Operation}\nException:{args.Exception}";
            await SendEventInfoAsync(errorInfoString);

        }

        public HubCallerContext Context {get; set;}
        public IHubCallerClients Clients {get; set;}


        public EventProcessorController()
        {                  

            _blobContainerClient = new BlobContainerClient(kStorageConnectionString,
                                                           kBlobContainerNameString);

            _eventProcessorClient = new EventProcessorClient
                (_blobContainerClient, kConsumerGroupNameString, kEventHubConnectionString);                  

        }

        public async Task OnConnectedAsync()
        {

            await PrepareProcessorAsync();

        }

        public async Task OnDisconnectedAsync()
        {

            await StopProcessorAsync("Stopped");

        }

        public async Task SendEventInfoAsync(string eventInfoString)
        {

            Console.WriteLine($"ConnectionId:{Context.ConnectionId}");
            await Clients.Caller.SendAsync("EventInfo", eventInfoString);
            

        }

        public async Task SendErrorInfoAsync(string errorInfoString)
        {

            await Clients.Caller.SendAsync("ErrorInfo", errorInfoString);            
        }

        public async Task StopProcessorAsync(string stopMessageString)
        {

            await _eventProcessorClient.StopProcessingAsync();
            await Clients.All.SendAsync("StopProcessor", stopMessageString);

        }
    }
}
