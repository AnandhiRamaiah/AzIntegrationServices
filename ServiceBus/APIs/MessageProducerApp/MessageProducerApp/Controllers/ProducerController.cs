using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Azure.Messaging.ServiceBus;
using MessageProducerApp.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace MessageProducerApp.Controllers
{

    [Route("api/[controller]")]
    public class ProducerController : Controller
    {

        private static ServiceBusClient kServiceBusClient;

        private static async Task<bool>
            SendServiceMessageAsync(string queueNameString, HeaderModel headerModel,
                                    string messageBody)
        {

            if (kServiceBusClient == null)
                kServiceBusClient = new ServiceBusClient(headerModel.ConnectionString);

            var serviceBusSender = kServiceBusClient.CreateSender(queueNameString);
            var messageBatch = await serviceBusSender.CreateMessageBatchAsync();
            var serviceMessage = new ServiceBusMessage(messageBody);
            var couldProcess = messageBatch.TryAddMessage(serviceMessage);

            if (couldProcess == false)
                return couldProcess;

            couldProcess = false;

            try
            {

                await serviceBusSender.SendMessageAsync(serviceMessage);
                couldProcess = true;
            }            
            finally
            {

                await serviceBusSender.DisposeAsync();
                await kServiceBusClient.DisposeAsync();

            }

            return couldProcess;

        }

        // POST api/values
        [HttpPost]
        [Route("queue")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult>
        ProduceMessageInQueue(string queueNameString, [FromHeader] HeaderModel headerModel,
                              [FromBody] string messageBody)
        {


            var couldProcess = await SendServiceMessageAsync(queueNameString, headerModel,
                                                             messageBody);
            ResponseModel responseModel = null;

            if (couldProcess == false)
            {

                responseModel = new ResponseModel("Bad Request", 400);
                return BadRequest(responseModel);

            }

            responseModel = new ResponseModel("message batch sent", 200);
            return Ok(responseModel);

        }

        [HttpPost]
        [Route("topic")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult>
            ProduceMessageInTopic(string topicNameString,
                                  [FromHeader] HeaderModel headerModel,
                                  [FromBody] string messageBody)
        {


            var couldProcess = await SendServiceMessageAsync(topicNameString, headerModel,
                                                             messageBody);
            ResponseModel responseModel = null;

            if (couldProcess == false)
            {

                responseModel = new ResponseModel("Bad Request", 400);
                return BadRequest(responseModel);

            }

            responseModel = new ResponseModel("message batch sent", 200);
            return Ok(responseModel);


        }
    }
}
