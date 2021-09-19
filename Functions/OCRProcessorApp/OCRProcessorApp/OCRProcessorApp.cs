using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace OCRProcessorApp
{
    public static class OCRProcessorApp
    {
        [FunctionName("OCRProcessorApp")]
        public static void Run([ServiceBusTrigger("ocrtopic", "ocrsubscription",
                                Connection = "ServiceBusTopicConnection")]
                                string mySbMsg, ILogger log)
        {
            log.LogInformation($"C# ServiceBus topic trigger function processed message: {mySbMsg}");
        }
    }
}
