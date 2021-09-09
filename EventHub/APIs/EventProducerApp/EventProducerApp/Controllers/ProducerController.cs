using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using EventProducerApp.Models;
using Newtonsoft.Json;

namespace EventProducerApp.Controllers
{

    [Route("api/[controller]")]
    public class ProducerController : Controller
    {

        private static EventHubProducerClient kEventProducerClient;  

        private static List<EventData> PrepareAllEvents(List<EventModel> eventsList)
        {

            var eventDataList = new List<EventData>(eventsList.Count);            
            foreach (var eventBody in eventsList)
            {

                var eventBodyString = JsonConvert.SerializeObject(eventBody);
                var eventData = new EventData(eventBodyString);                 
                eventDataList.Add(eventData);

            }

            return eventDataList;

        }
        
        private static async Task<ResponseModel> PerformProduceEventsAsync
            (string ehNameString, HeaderModel headerModel, List<EventModel> eventsList)
        {

            if (kEventProducerClient == null)
                kEventProducerClient = new EventHubProducerClient(headerModel.ConnectionString,
                                                                  ehNameString);

            var maxSizeInBytes = (long)(headerModel.MaxSizeInMB * 1024 * 1024);
            var partitionId = headerModel.PartitionId;
            var partitionKey = headerModel.PartitionKey;

            var eventDataList = PrepareAllEvents(eventsList);
            EventDataBatch eventHubBatch = null;
            ResponseModel responseModel = null;

            try
            {

                eventHubBatch = await kEventProducerClient.CreateBatchAsync(new CreateBatchOptions()
                {

                    MaximumSizeInBytes = maxSizeInBytes,
                    PartitionId = ((partitionId != null) && (partitionId.Equals(string.Empty) == false))
                                ? partitionId : string.Empty,
                    PartitionKey = ((partitionKey != null) && (partitionKey.Equals(string.Empty) == false))
                                ? partitionKey : string.Empty

                });

                foreach (var eventData in eventDataList)
                    eventHubBatch.TryAdd(eventData);

                await kEventProducerClient.SendAsync(eventHubBatch);
                responseModel = new ResponseModel()
                {

                    Code = 200,
                    Message = $"event batch sent:{eventDataList.Count}"

                };
            }
            catch(EventHubsException ex)
            {

                responseModel = new ResponseModel()
                {

                    Code = 400,
                    Message = ex.Message

                };
            }
            finally
            {
                eventHubBatch?.Dispose();
            }

            return responseModel;

        }        

        [HttpPost]
        [Route("eventhub/{ehNameString}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult>
        ProduceEventAsync(string ehNameString, [FromHeader] HeaderModel headerModel,
                          [FromBody] List<EventModel> eventsList)
        {
           
            var responseModel = await PerformProduceEventsAsync(ehNameString, headerModel,
                                                                eventsList);            
            return Ok(responseModel);

        }        
    }
}
