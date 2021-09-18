using System;
using System.Transactions;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Management.ServiceBus;
using MessageProducerApp.Models;
using Newtonsoft.Json;

namespace MessageProducerApp.Controllers
{

    [Route("api/[controller]")]
    public class ProducerController : Controller
    {

        private ServiceBusClient kServiceBusClient;
        private ServiceBusManagementClient kServiceBusManagementClient;

        private static List<ServiceBusMessage> PrepareAllQueueMessages(List<MessageModel> messagesList)
        {

            var serviceBusMessagesList = new List<ServiceBusMessage>(messagesList.Count);            
            foreach (var messageBody in messagesList)
            {

                var messageBodyString = JsonConvert.SerializeObject(messageBody);
                var serviceBusMessage = new ServiceBusMessage(messageBodyString);

                var partitionKeyString = messageBody.Name;
                var messageIdString = messageBody.MessageId;
                serviceBusMessage.PartitionKey = partitionKeyString;
                // serviceBusMessage.MessageId = messageIdString;

                 serviceBusMessagesList.Add(serviceBusMessage);

            }

            return serviceBusMessagesList;

        }

        private static List<ServiceBusMessage> PrepareAllOCRMessages(List<OCRModel> ocrList)
        {

            var serviceBusMessagesList = new List<ServiceBusMessage>(ocrList.Count);
            foreach (var ocrBody in ocrList)
            {

                var ocrBodyString = JsonConvert.SerializeObject(ocrBody);
                var partitionKeyString = ocrBody.Domain;
                var messageIdString = ocrBody.OCRId;                

                var serviceBusMessage = new ServiceBusMessage(ocrBodyString)
                {
                    SessionId = ocrBody.Domain

                };
                serviceBusMessage.PartitionKey = partitionKeyString;
                // serviceBusMessage.MessageId = messageIdString;   
                serviceBusMessagesList.Add(serviceBusMessage);

            }

            return serviceBusMessagesList;

        }

        private async Task<ResponseModel> SendQueueMessageAsync
            (string queueNameString, HeaderModel headerModel, List<MessageModel> messagesList)
        {

            kServiceBusClient = new ServiceBusClient(headerModel.ConnectionString);
            var serviceBusSender = kServiceBusClient.CreateSender(queueNameString);            
            var serviceBusMessagesList = PrepareAllQueueMessages(messagesList);
            ResponseModel responseModel = null;

            try
            {

                await serviceBusSender.SendMessagesAsync(serviceBusMessagesList);
                responseModel = new ResponseModel()
                {

                    Code = 200,
                    Message = $"message batch sent:{serviceBusMessagesList.Count}"

                };
            }
            catch(ServiceBusException ex)
            {

                responseModel = new ResponseModel()
                {

                    Code = 400,
                    Message = ex.Message

                };
            }
            finally
            {
                await serviceBusSender.DisposeAsync();
            }

            return responseModel;

        }

        private async Task<List<ResponseModel>> ScheduleQueueMessageAsync
            (string queueNameString, HeaderModel headerModel, List<MessageModel> messagesList,
            Dictionary<string, int> queryStringMap)
        {

            kServiceBusClient = new ServiceBusClient(headerModel.ConnectionString);
            var serviceBusSender = kServiceBusClient.CreateSender(queueNameString);            
            var serviceBusMessagesList = PrepareAllQueueMessages(messagesList);                
            int delayMinutes = (int)(queryStringMap["delayBy"])/60;
            long scheduleSequence = 0;            
            var responseModelsList = new List<ResponseModel>();

            try
            {

                
                var scheduledTasksList = serviceBusMessagesList.Select(
                    async (ServiceBusMessage serviceBusMessage) =>
                {

                    scheduleSequence = await serviceBusSender.ScheduleMessageAsync(
                                                serviceBusMessage,
                                                DateTimeOffset.Now.AddMinutes(delayMinutes));
                    var responseModel = new ResponseModel()
                    {

                        Code = 200,
                        Message = $"message scheduled:{scheduleSequence}"

                    };
                    responseModelsList.Add(responseModel);

                }).ToList();

                await Task.WhenAll(scheduledTasksList);
                
            }
            catch (ServiceBusException ex)
            {

                var responseModel = new ResponseModel()
                {

                    Code = 400,
                    Message = ex.Message

                };
                responseModelsList.Add(responseModel);

            }
            finally
            {
                await serviceBusSender.DisposeAsync();
            }

            return responseModelsList;

        }

        private async Task<bool> SendTopicMessageAsync
           (string topicNameString, HeaderModel headerModel, List<OCRModel> ocrList)
        {

           kServiceBusClient = new ServiceBusClient(headerModel.ConnectionString);
           var serviceBusSender = kServiceBusClient.CreateSender(topicNameString);
           var serviceBusMessagesList = PrepareAllOCRMessages(ocrList);
           bool couldProcess = false;

           try
           {

               await serviceBusSender.SendMessagesAsync(serviceBusMessagesList);
               couldProcess = true;

           }
           finally
           {
               await serviceBusSender.DisposeAsync();
           }

           return couldProcess;

        }

        [HttpPost]
        [Route("queue/{queueNameString}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult>
        ProduceMessageInQueue(string queueNameString, [FromHeader] HeaderModel headerModel,
                              [FromBody] List<MessageModel> messagesList)
        {
           
            var responseModel = await SendQueueMessageAsync(queueNameString, headerModel,
                                                             messagesList);            
            return Ok(responseModel);

        }
        
        [HttpPost]
        [Route("schedule/queue/{queueNameString}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult>
        ScheduleMessageInQueue(string queueNameString, [FromHeader] HeaderModel headerModel,
                              [FromBody] List<MessageModel> messagesList,
                              [FromQuery] Dictionary<string, int> queryStringMap)
        {
           
            var responseModelsList = await ScheduleQueueMessageAsync(queueNameString, headerModel,
                                                                     messagesList, queryStringMap);            
            return Ok(responseModelsList);

        }

        [HttpPost]
        [Route("topic/{topicNameString}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult>
           ProduceMessageInTopic(string topicNameString, [FromHeader] HeaderModel headerModel,
                                 [FromBody] List<OCRModel> ocrList)
        {

           var couldProcess = await SendTopicMessageAsync(topicNameString, headerModel, ocrList);
           ResponseModel responseModel = null;

           if (couldProcess == false)
           {

               responseModel = new ResponseModel()
               {

                   Code = 400,
                   Message = "Bad Request"

               };
               return BadRequest(responseModel);

           }

           responseModel = new ResponseModel()
            {

                Code = 200,
                Message = $"message batch sent:{ocrList.Count}"

            };
           return Ok(responseModel);
            
        }
    }
}
