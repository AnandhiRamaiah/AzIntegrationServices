using System;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using EventReceiverApp.Models;
using Newtonsoft.Json;

namespace EventReceiverApp.Controllers
{
    [Route("api/[controller]")]
    public class ReceiverController : Controller
    {
        private static EventHubConsumerClient kEventHubClient;
        private static TimeSpan kWaitTimeSpan = TimeSpan.FromMilliseconds(2000);

        [HttpGet]
        [Route("eventhub/{ehNameString}/{cgNameString}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ReceiveFromEventHubAsync
            (string ehNameString, string cgNameString, [FromHeader] HeaderModel headerModel)
        {

            if (kEventHubClient == null)
            {

                kEventHubClient = new EventHubConsumerClient
                    (cgNameString, headerModel.ConnectionString, ehNameString);

            }

            List<EventModel> eventModelsList = new List<EventModel>();
            ErrorModel errorModel = null;
            string eventDataString = null; 

            try
            {
                
                var readEventOptions = new ReadEventOptions()
                {

                    MaximumWaitTime = kWaitTimeSpan                    
                    
                };

                await foreach(var partitionEvent in kEventHubClient.ReadEventsAsync(true, readEventOptions))
                {

                    if (partitionEvent.Data == null)
                        break;

                    eventDataString = Encoding.UTF8.GetString(partitionEvent.Data?.EventBody.ToArray());
                    var eventModel = JsonConvert.DeserializeObject<EventModel>(eventDataString);
                    eventModelsList.Add(eventModel);

                }                
            }            
            catch (EventHubsException ex)
            {

                errorModel = new ErrorModel()
                {

                    Code = 500,
                    Message = ex.Message

                };
            }
            catch(JsonSerializationException ex)
            {

                errorModel = new ErrorModel()
                {

                    Code = 400,
                    Message = eventDataString

                };

            }

            return Ok((errorModel != null) ? errorModel : eventModelsList);

        }

        [HttpGet]
        [Route("eventhub/{ehNameString}/{cgNameString}/{partitionIdString}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ReceiveFromEventHubPartitionAsync
            (string ehNameString, string cgNameString, string partitionIdString,
            [FromHeader] HeaderModel headerModel)
        {

            if (kEventHubClient == null)
            {

                kEventHubClient = new EventHubConsumerClient
                    (cgNameString, headerModel.ConnectionString, ehNameString);

            }

            List<EventModel> eventModelsList = new List<EventModel>();
            ErrorModel errorModel = null;

            try
            {

                var readEventOptions = new ReadEventOptions()
                {

                    MaximumWaitTime = kWaitTimeSpan

                };
                
                var partitionProperties =
                    await kEventHubClient.GetPartitionPropertiesAsync(partitionIdString);
                var position = EventPosition.FromOffset(partitionProperties.LastEnqueuedOffset);

                await foreach (var partitionEvent in kEventHubClient.ReadEventsFromPartitionAsync
                    (partitionIdString, position, readEventOptions))
                {

                    if (partitionEvent.Data == null)
                        break;

                    var eventDataString = Encoding.UTF8.GetString(partitionEvent.Data.EventBody
                                                       .ToArray());
                    var eventModel = JsonConvert.DeserializeObject<EventModel>(eventDataString);
                    eventModelsList.Add(eventModel);

                }
            }
            catch (EventHubsException ex)
            {

                errorModel = new ErrorModel()
                {

                    Code = 500,
                    Message = ex.Message

                };
            }

            return Ok((errorModel != null) ? errorModel : eventModelsList);

        }
    }
}
