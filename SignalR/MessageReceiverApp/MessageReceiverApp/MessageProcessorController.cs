using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.SignalR;
using Azure.Messaging.ServiceBus;

namespace MessageReceiverApp
{
    public class MessageProcessorController
    {

        private static string kServiceBusQueueConnectionString = "Endpoint=sb://socialstdns.servicebus.windows.net/;SharedAccessKeyName=tweetqueue-listen-rule;SharedAccessKey=AivzQUOtYddt96rS2T4YbZxnkZ7nC4C6N5ehX+08KWw=;EntityPath=tweetqueue";
        private static string kServiceBusTopicConnectionString = "Endpoint=sb://socialstdns.servicebus.windows.net/;SharedAccessKeyName=ocrtopic-listen-rule;SharedAccessKey=Zq5Om85iu3gSrH0tPDyazz5feK3PXRIWMaZhIqfido4=;EntityPath=ocrtopic";
        private static string kServiceBusQueueString = "tweetqueue";
        private static string kServiceBusTopicString = "ocrtopic";
        private static string kServiceBusSubscriptionString = "ocrsubscription";
        private ServiceBusClient _serviceBusClient;
        private ServiceBusClient _serviceBusSessionClient;
        private ServiceBusProcessor _serviceBusProcessor;
        private ServiceBusSessionProcessor _serviceBusSessionProcessor;

        private async Task PrepareProcessorAsync()
        {

            var serviceBusProcessorOptions = new ServiceBusProcessorOptions
            {
                
                AutoCompleteMessages = false,
                MaxConcurrentCalls = 2

            };

             _serviceBusProcessor = _serviceBusClient.CreateProcessor
            (kServiceBusQueueString, serviceBusProcessorOptions);


            var serviceBusSessionProcessorOptions = new ServiceBusSessionProcessorOptions()
            {
                
                AutoCompleteMessages = false,
                PrefetchCount = 2,                
                MaxConcurrentCallsPerSession = 2                

            };

            _serviceBusSessionProcessor = _serviceBusSessionClient.CreateSessionProcessor
                (kServiceBusTopicString, kServiceBusSubscriptionString,
                serviceBusSessionProcessorOptions);                           

            _serviceBusProcessor.ProcessMessageAsync += ProcessServiceBusHanlderAsync;
            _serviceBusProcessor.ProcessErrorAsync += ProcessErrorHandlerAsync;

            _serviceBusSessionProcessor.ProcessMessageAsync += ProcessSessionServiceBusHanlderAsync;
            _serviceBusSessionProcessor.ProcessErrorAsync += ProcessSessionErrorHandlerAsync;

            await _serviceBusProcessor.StartProcessingAsync();
            await _serviceBusSessionProcessor.StartProcessingAsync();
        }

        private async Task ProcessServiceBusHanlderAsync(ProcessMessageEventArgs args)
        {

            string messageInfoString = args.Message.Body.ToString();                       
            await args.CompleteMessageAsync(args.Message);
            await SendEventInfoAsync(messageInfoString);

        }

        private async Task ProcessErrorHandlerAsync(ProcessErrorEventArgs args)
        {

            var errorInfoString = $"ErrorInfo:{args.ErrorSource}\nException:{args.Exception}";
            await SendEventInfoAsync(errorInfoString);

        }

        private async Task ProcessSessionServiceBusHanlderAsync(ProcessSessionMessageEventArgs args)
        {

            string messageInfoString = args.Message.Body.ToString();                       
            await args.CompleteMessageAsync(args.Message);
            await SendEventInfoAsync(messageInfoString);

        }

        private async Task ProcessSessionErrorHandlerAsync(ProcessErrorEventArgs args)
        {

            var errorInfoString = $"ErrorInfo:{args.ErrorSource}\nException:{args.Exception}";
            await SendEventInfoAsync(errorInfoString);

        }

        public HubCallerContext Context { get; set; }
        public IHubCallerClients Clients { get; set; }

        public MessageProcessorController()
        {

            var serviceBusClientOptions = new ServiceBusClientOptions()
            {

                EnableCrossEntityTransactions = false,
                TransportType = ServiceBusTransportType.AmqpTcp,
                RetryOptions = new ServiceBusRetryOptions()
                {

                    Delay = TimeSpan.FromSeconds(1),
                    MaxRetries = 3,
                    Mode = ServiceBusRetryMode.Exponential,
                    MaxDelay = TimeSpan.FromSeconds(10),
                    TryTimeout = TimeSpan.FromSeconds(20)                    

                }
            };

            _serviceBusClient = new ServiceBusClient(kServiceBusQueueConnectionString,
                                                     serviceBusClientOptions);

            _serviceBusSessionClient = new ServiceBusClient(kServiceBusTopicConnectionString,
                                                            serviceBusClientOptions);

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
            await Clients.Caller.SendAsync("MessageInfo", eventInfoString);
        }

        public async Task SendErrorInfoAsync(string errorInfoString)
        {

            await Clients.Caller.SendAsync("ErrorInfo", errorInfoString);
        }

        public async Task StopProcessorAsync(string stopMessageString)
        {

            await _serviceBusProcessor.StopProcessingAsync();
            await Clients.All.SendAsync("StopProcessor", stopMessageString);
        }
    }
}
