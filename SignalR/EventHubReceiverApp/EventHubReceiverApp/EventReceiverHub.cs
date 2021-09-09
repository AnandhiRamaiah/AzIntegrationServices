using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace EventHubReceiverApp
{
    public class EventReceiverHub : Hub
    {
        
        private readonly EventProcessorController _eventProcessorController;
        public EventReceiverHub(EventProcessorController eventProcessorController)
        {   
                                                    
            _eventProcessorController = eventProcessorController;            

        }

        public override async Task OnConnectedAsync()
        {

            _eventProcessorController.Context = Context;
            _eventProcessorController.Clients = Clients;
            await _eventProcessorController.OnConnectedAsync();
            await base.OnConnectedAsync();

        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            
            await _eventProcessorController.OnDisconnectedAsync();
            await base.OnDisconnectedAsync(exception);

        }            
    }
}
