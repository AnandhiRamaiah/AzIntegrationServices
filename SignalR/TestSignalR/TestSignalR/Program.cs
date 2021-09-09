using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace TestSignalR
{
    class Program
    {
        static async Task Main(string[] args)
        {

            var eventHubConnection = new HubConnectionBuilder().WithUrl("http://localhost:5000/eventhub")
                                                          .Build();
                                                          
            var serviceBusConnection = new HubConnectionBuilder().WithUrl("http://localhost:6000/servicebus")
                                                          .Build();

            eventHubConnection.On<string>("EventInfo", (eventInfoString) =>
            {

                Console.WriteLine($"Event Info:{eventInfoString}");                

            });

            eventHubConnection.On<string>("ErrorInfo", (errorInfoString) =>
            {

                Console.WriteLine($"Error Info:{errorInfoString}");

            });

            eventHubConnection.On<string>("StopProcessor",  (message) =>
            {
                
                Console.WriteLine($"Message:{message}");

            });

            serviceBusConnection.On<string>("MessageInfo", (messageInfoString) =>
            {

                Console.WriteLine($"Event Info:{messageInfoString}");                

            });

            serviceBusConnection.On<string>("ErrorInfo", (errorInfoString) =>
            {

                Console.WriteLine($"Error Info:{errorInfoString}");

            });

            serviceBusConnection.On<string>("StopProcessor",  (message) =>
            {
                
                Console.WriteLine($"Message:{message}");

            });

            // await eventHubConnection.StartAsync();
            await serviceBusConnection.StartAsync();
            Console.ReadKey();
            // await eventHubConnection.StopAsync();
            await serviceBusConnection.StopAsync();



        }
    }
}
