using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace MessageReceiverApp
{
    public class MessageReceiverHub : Hub
    {

        private readonly MessageProcessorController _messageProcessorController;

        public MessageReceiverHub(MessageProcessorController messageProcessorController)
        {

            _messageProcessorController = messageProcessorController;            

        }

        public override async Task OnConnectedAsync()
        {

            _messageProcessorController.Context = Context;
            _messageProcessorController.Clients = Clients;
            await _messageProcessorController.OnConnectedAsync();
            await base.OnConnectedAsync();

        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {

            await _messageProcessorController.OnDisconnectedAsync();
            await base.OnDisconnectedAsync(exception);

        }
    }
}
