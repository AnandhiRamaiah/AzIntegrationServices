using System;
using System.Text;
using System.Transactions;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Azure.Messaging.ServiceBus;
using MessageReceiverApp.Models;
using Newtonsoft.Json;

namespace MessageReceiverApp.Controllers
{
    [Route("api/receiver")]
    public class TopicReceiverController : Controller
    {

        private ServiceBusClient kServiceBusClient;
        private static TimeSpan kWaitTimeSpan = TimeSpan.FromMilliseconds(500);

        [HttpGet]
        [Route("topic/{topicNameString}/subscription/{subscriptionNameString}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ReceiveFromTopicAsync
            (string topicNameString, string subscriptionNameString,
            [FromHeader] HeaderModel headerModel,
            [FromQuery] Dictionary<string, string> queryStringMap)
        {

            kServiceBusClient = new ServiceBusClient(headerModel.ConnectionString);
            ServiceBusSessionReceiver sessionReceiver = null;
            OCRModel receivedModel = null;
            ErrorModel errorModel = null;

            try
            {

                var shouldForce = (queryStringMap != null) && queryStringMap["force"].Equals("true");
                var sessionNameString = queryStringMap["session"];

                sessionReceiver = await kServiceBusClient.AcceptSessionAsync
                (topicNameString, subscriptionNameString, sessionNameString);

                var receivedMessage = await sessionReceiver?.ReceiveMessageAsync(kWaitTimeSpan);
                if (receivedMessage == null)
                    throw new ArgumentNullException(nameof(receivedMessage));

                receivedModel = JsonConvert.DeserializeObject<OCRModel>
                                   (Encoding.UTF8.GetString(receivedMessage.Body));
                if (receivedModel == null)
                    throw new ArgumentNullException(nameof(receivedModel));

                if (receivedModel.OCRId.StartsWith("dl") == true)
                    await sessionReceiver?.DeadLetterMessageAsync(receivedMessage);
                else if ((receivedModel.OCRId.StartsWith("ab") == true)
                         && (shouldForce == false))
                    await sessionReceiver?.AbandonMessageAsync(receivedMessage);
                else if (receivedModel.OCRId.StartsWith("df") == true)
                {
                    receivedModel.SequenceNumber = receivedMessage.SequenceNumber;
                    await sessionReceiver?.DeferMessageAsync(receivedMessage);
                }
                else
                    await sessionReceiver?.CompleteMessageAsync(receivedMessage);

            }
            catch (ArgumentNullException ex)
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

                if (sessionReceiver != null)
                    await sessionReceiver.DisposeAsync();

            }

            return Ok((receivedModel != null) ? receivedModel : errorModel);

        }

        [HttpGet]
        [Route("deadletter/topic/{topicNameString}/subscription/{subscriptionNameString}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ReadFromDeadLetterTopicAsync
            (string topicNameString, string subscriptionNameString,
            [FromHeader] HeaderModel headerModel)
        {

            kServiceBusClient = new ServiceBusClient(headerModel.ConnectionString);
            var deadLetterReceiver = kServiceBusClient.CreateReceiver
                (topicNameString, subscriptionNameString, new ServiceBusReceiverOptions()
             {

                 SubQueue = SubQueue.DeadLetter,
                 ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete

             });            
            
            OCRModel receivedModel = null;
            ErrorModel errorModel = null;

            try
            {

                var receivedMessage = await deadLetterReceiver?.ReceiveMessageAsync(kWaitTimeSpan);
                if (receivedMessage == null)
                    throw new ArgumentNullException(nameof(receivedMessage));

                receivedModel = JsonConvert.DeserializeObject<OCRModel>
                                    (Encoding.UTF8.GetString(receivedMessage.Body));
                if (receivedModel == null)
                    throw new ArgumentNullException(nameof(receivedModel));

            }
            catch (ArgumentNullException ex)
            {

                errorModel = new ErrorModel()
                {

                    Code = 500,
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

                if (deadLetterReceiver != null)
                    await deadLetterReceiver.DisposeAsync();

            }

            return Ok((receivedModel != null) ? receivedModel : errorModel);

        }

        [HttpGet]
        [Route("defer/topic/{topicNameString}/subscription/{subscriptionNameString}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ReadFromDeferredTopicAsync
            (string topicNameString, string subscriptionNameString,
            [FromHeader] HeaderModel headerModel,
            [FromQuery] Dictionary<string, string> queryStringMap)
        {

            kServiceBusClient = new ServiceBusClient(headerModel.ConnectionString);
            var serviceBusSessionReceiverOptions = new ServiceBusSessionReceiverOptions()
            {

                PrefetchCount = 2,
                ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete

            };

            ServiceBusSessionReceiver sessionReceiver = null;
            OCRModel receivedModel = null;
            ErrorModel errorModel = null;

            try
            {

                var deferredSequenceNumber = long.Parse(queryStringMap["sequence"]);
                var sessionNameString = queryStringMap?["session"];

                sessionReceiver = await kServiceBusClient.AcceptSessionAsync
                (topicNameString, subscriptionNameString, sessionNameString,
                serviceBusSessionReceiverOptions);
                
                var receivedMessage = await sessionReceiver?.ReceiveDeferredMessageAsync
                                            (deferredSequenceNumber);

                if (receivedMessage == null)
                    throw new ArgumentNullException(nameof(receivedMessage));

                receivedModel = JsonConvert.DeserializeObject<OCRModel>
                                    (Encoding.UTF8.GetString(receivedMessage.Body));
                if (receivedModel == null)
                    throw new ArgumentNullException(nameof(receivedModel));

                receivedModel.SequenceNumber = deferredSequenceNumber;

            }
            catch (ArgumentNullException ex)
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

                if (sessionReceiver != null)
                    await sessionReceiver.DisposeAsync();

            }

            return Ok((receivedModel != null) ? receivedModel : errorModel);

        }

        [HttpPost]
        [Route("forward/topic/{topicNameString}/subscription/{subscriptionNameString}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ForwardToTopicAsync
            (string topicNameString, string subscriptionNameString,
            [FromHeader] ForwardHeaderModel forwardHeaderModel,
            [FromQuery] Dictionary<string, string> queryStringMap)
        {

            var serviceBusClientOptions = new ServiceBusClientOptions()
            {

                EnableCrossEntityTransactions = true,                
                TransportType = ServiceBusTransportType.AmqpTcp

            };

            kServiceBusClient = new ServiceBusClient(forwardHeaderModel.ConnectionString,
                                                     serviceBusClientOptions);
            var serviceBusReceiverOptions = new ServiceBusReceiverOptions()
            {

                PrefetchCount = 2,
                ReceiveMode = ServiceBusReceiveMode.PeekLock              

            };

            ServiceBusReceiver serviceBusReceiver = null;
            ServiceBusSender nextHopSender = null;
            OCRModel receivedModel = null;
            ErrorModel errorModel = null;

            try
            {

                var shouldForce = (queryStringMap != null) && queryStringMap["force"].Equals("true");
                var sessionNameString = queryStringMap["session"];
                
                var nextHopTopicNameString = forwardHeaderModel.NextHopTopicName;                
                var nextHopSessionNameString = forwardHeaderModel.NextHopSessionName;

                serviceBusReceiver = kServiceBusClient.CreateReceiver(
                                        topicNameString, subscriptionNameString,
                                        serviceBusReceiverOptions);
                nextHopSender = kServiceBusClient.CreateSender(nextHopTopicNameString);

                var receivedMessage = await serviceBusReceiver?.ReceiveMessageAsync(kWaitTimeSpan);                                            
                if (receivedMessage == null)
                    throw new ArgumentNullException(nameof(receivedMessage));

                receivedModel = JsonConvert.DeserializeObject<OCRModel>
                                    (Encoding.UTF8.GetString(receivedMessage.Body));
                if (receivedModel == null)
                    throw new ArgumentNullException(nameof(receivedModel));

                using (var ts = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {

                    await serviceBusReceiver.CompleteMessageAsync(receivedMessage);

                    var serviceBusMessage = new ServiceBusMessage(receivedMessage);
                    serviceBusMessage.TransactionPartitionKey = receivedMessage.PartitionKey;
                    serviceBusMessage.SessionId = nextHopSessionNameString;
                    
                    await nextHopSender.SendMessageAsync(serviceBusMessage);
                    ts.Complete();

                }
            }
            catch (ArgumentNullException ex)
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

                if (serviceBusReceiver != null)
                    await serviceBusReceiver.DisposeAsync();

                if (nextHopSender != null)
                    await nextHopSender.DisposeAsync();

            }

            return Ok((receivedModel != null) ? receivedModel : errorModel);

        }
    }
}
