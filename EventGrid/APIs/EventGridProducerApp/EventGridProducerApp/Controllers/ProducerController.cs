using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Azure;
using Azure.Messaging.EventGrid;
using EventGridProducerApp.Models;
using Newtonsoft.Json;

namespace EventGridProducerApp.Controllers
{
    [Route("api/[controller]")]
    public class ProducerController : Controller
    {

        private static EventGridPublisherClient kEventGridPublisherClient;

        [HttpPost]
        [Route("eventgrid")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult>
        ProduceEventGridEventAsync([FromHeader] HeaderModel headerModel,
                                   [FromBody] List<EventModel> eventsList)
        {

            if (kEventGridPublisherClient == null)
            {

                var topicEndpointString = headerModel.TopicEndpointString;
                var accesskeyString = headerModel.AccessKeyString;

                var uri = new Uri(topicEndpointString);
                var creds = new AzureKeyCredential(accesskeyString);

                kEventGridPublisherClient = new EventGridPublisherClient(uri, creds);

            }

            var eventGridEventsList = new List<EventGridEvent>();
            foreach(var eventModel in eventsList)
            {

                var eventGridEventModel = new EventGridEvent("sub", "topic", "1.0", eventModel);
                eventGridEventsList.Add(eventGridEventModel);

            }

            var response = await kEventGridPublisherClient.SendEventsAsync(eventGridEventsList);
            return Ok();

        }
    }
}
