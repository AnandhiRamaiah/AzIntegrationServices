using System;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Azure.Messaging.ServiceBus;
using MessageReceiverApp.Models;
using Newtonsoft.Json;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace MessageReceiverApp.Controllers
{

    [Route("api/receiver")]
    public class QueueReceiverController : Controller
    {

        private static ServiceBusClient kServiceBusClient;
        private static TimeSpan kWaitTimeSpan = TimeSpan.FromMilliseconds(500);

        [HttpGet]
        [Route("queue/{queueNameString}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ReceiveFromQueue(string queueNameString,
                                                          [FromHeader] HeaderModel headerModel,
                                                          [FromQuery] Dictionary<string, string>
                                                          queryStringMap)
        {

            if (kServiceBusClient == null)
                kServiceBusClient = new ServiceBusClient(headerModel.ConnectionString);

            var shouldForce = (queryStringMap != null) && queryStringMap["force"].Equals("true");
            var receiver = kServiceBusClient.CreateReceiver(queueNameString);            
            MessageModel receivedModel = null;
            ErrorModel errorModel = null;            

            try
            {

                var receivedMessage = await receiver.ReceiveMessageAsync(kWaitTimeSpan);
                if (receivedMessage == null)
                    throw new ArgumentNullException(nameof(receivedMessage));

                receivedModel = JsonConvert.DeserializeObject<MessageModel>
                                   (Encoding.UTF8.GetString(receivedMessage.Body));
                if (receivedModel == null)
                    throw new ArgumentNullException(nameof(receivedModel));   

                if (receivedModel.MessageId.StartsWith("dl") == true)
                    await receiver.DeadLetterMessageAsync(receivedMessage);
                else if ((receivedModel.MessageId.StartsWith("ab") == true)
                        && (shouldForce == false))
                    await receiver.AbandonMessageAsync(receivedMessage);
                else if (receivedModel.MessageId.StartsWith("df") == true)
                {
                    receivedModel.SequenceNumber = receivedMessage.SequenceNumber;
                    await receiver.DeferMessageAsync(receivedMessage);
                }                    
                else
                    await receiver.CompleteMessageAsync(receivedMessage);
            }
            catch(ArgumentNullException ex)
            {

                errorModel = new ErrorModel()
                {

                    Code = 400,
                    Message = ex.Message

                };
            }
            catch (ServiceBusException ex)
            {

                errorModel = new ErrorModel()
                {

                    Code = 500,
                    Message = ex.Message

                };
            }
            finally
            {

                await receiver.DisposeAsync();

            }

            return Ok((receivedModel != null) ? receivedModel : errorModel);
        }

        [HttpGet]
        [Route("queue/{queueNameString}/peek")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PeekFromQueue(string queueNameString,
                                                          [FromHeader] HeaderModel headerModel,
                                                          [FromQuery] Dictionary<string, string>
                                                          queryStringMap)
        {

            if (kServiceBusClient == null)
                kServiceBusClient = new ServiceBusClient(headerModel.ConnectionString);

            var serviceBusReceiverOptions = new ServiceBusReceiverOptions()
            {
                ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete
            };

            var receiver = kServiceBusClient.CreateReceiver(queueNameString,
                                                            serviceBusReceiverOptions);            
            MessageModel receivedModel = null;
            ErrorModel errorModel = null;
            var receivedModelsList = new List<MessageModel>();

            try
            {

                var receivedMessagesList = await receiver.PeekMessagesAsync(5);
                foreach (var receivedMessage in receivedMessagesList)
                {

                    receivedModel = JsonConvert.DeserializeObject<MessageModel>
                                   (Encoding.UTF8.GetString(receivedMessage.Body));
                    if (receivedModel == null)
                    throw new ArgumentNullException(nameof(receivedModel)); 

                    receivedModel.SequenceNumber = receivedMessage.SequenceNumber;
                    receivedModelsList.Add(receivedModel);

                }                                
            }
            catch(ArgumentNullException ex)
            {

                errorModel = new ErrorModel()
                {

                    Code = 400,
                    Message = ex.Message

                };
            }
            catch (ServiceBusException ex)
            {

                errorModel = new ErrorModel()
                {

                    Code = 500,
                    Message = ex.Message

                };
            }
            finally
            {

                await receiver.DisposeAsync();

            }

            return Ok((receivedModelsList.Count > 0) ? receivedModelsList : errorModel);
        }

        [HttpGet]
        [Route("deadletter/queue/{queueNameString}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ReadFromDeadLetterQueue
            (string queueNameString, [FromHeader] HeaderModel headerModel)
        {

            if (kServiceBusClient == null)
                kServiceBusClient = new ServiceBusClient(headerModel.ConnectionString);

             var deadLetterReceiver = kServiceBusClient.CreateReceiver(queueNameString,
             new ServiceBusReceiverOptions()
             {

                 SubQueue = SubQueue.DeadLetter,
                 ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete

             });

            MessageModel receivedModel = null;
            ErrorModel errorModel = null;

            try
            {

                var receivedMessage = await deadLetterReceiver.ReceiveMessageAsync(kWaitTimeSpan);
                if (receivedMessage == null)
                    throw new ArgumentNullException(nameof(receivedMessage));

                receivedModel = JsonConvert.DeserializeObject<MessageModel>
                                    (Encoding.UTF8.GetString(receivedMessage.Body));
                if (receivedModel == null)
                    throw new ArgumentNullException(nameof(receivedModel));

            }
            catch(ArgumentNullException ex)
            {

                errorModel = new ErrorModel()
                {

                    Code = 500,
                    Message = ex.Message

                };

            }
            finally
            {

                await deadLetterReceiver.DisposeAsync();

            }

            return Ok((receivedModel != null) ? receivedModel : errorModel);

        }

        [HttpGet]
        [Route("defer/queue/{queueNameString}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ReadFromDeferredQueue
            (string queueNameString, [FromHeader] HeaderModel headerModel,
            [FromQuery] Dictionary<string, long> queryStringMap)
        {

            if (kServiceBusClient == null)
                kServiceBusClient = new ServiceBusClient(headerModel.ConnectionString);

            var deferredReceiver = kServiceBusClient.CreateReceiver(queueNameString,
            new ServiceBusReceiverOptions()
            {

                PrefetchCount = 2,
                ReceiveMode = ServiceBusReceiveMode.PeekLock

            });

            MessageModel receivedModel = null;
            ErrorModel errorModel = null;

            try
            {

                var deferredSequenceNumber = queryStringMap["sequence"];
                var receivedMessage = await deferredReceiver
                                            .ReceiveDeferredMessageAsync(deferredSequenceNumber);
                if (receivedMessage == null)
                    throw new ArgumentNullException(nameof(receivedMessage));

                receivedModel = JsonConvert.DeserializeObject<MessageModel>
                                    (Encoding.UTF8.GetString(receivedMessage.Body));
                if (receivedModel == null)
                    throw new ArgumentNullException(nameof(receivedModel));

                await deferredReceiver.CompleteMessageAsync(receivedMessage);
                receivedModel.SequenceNumber = deferredSequenceNumber;

            }
            catch (ArgumentNullException ex)
            {

                errorModel = new ErrorModel()
                {

                    Code = 500,
                    Message = ex.Message

                };

            }
            finally
            {

                await deferredReceiver.DisposeAsync();

            }

            return Ok((receivedModel != null) ? receivedModel : errorModel);

        }
    }
}
