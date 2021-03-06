using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;

namespace EventGridProcessorApp
{
    public static class EventGridProcessorApp
    {
        [FunctionName("EventGridProcessorApp")]
        public static void Run([EventGridTrigger] EventGridEvent eventGridEvent,
                                ILogger log)
        {
            log.LogInformation(eventGridEvent.Data.ToString());
        }
    }
}
