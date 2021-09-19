using System;
using System.Text;
using Microsoft.Azure.ServiceBus.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using TweetProocessorApp.Models;
using Newtonsoft.Json;

namespace TweetProocessorApp
{
    public static class TweetProocessorApp
    {

        [FunctionName("TweetProocessorApp")]
        [return: ServiceBus("tweetqueue", Connection = "SecondaryQueueConnection")]
        public static Message Run([ServiceBusTrigger("tweetqueue",
                                        Connection = "PrimaryQueueConnection")]
                                        Message message, ILogger log)
        {    

            var forwardMessage = message.Clone();
            var messageItem = JsonConvert.DeserializeObject<MessageModel>
            (Encoding.UTF8.GetString (forwardMessage.Body));

            log.LogInformation($"Name:{messageItem.Name}");
            log.LogInformation($"Tweet:{messageItem.Tweet}");

            return forwardMessage;

        }        
    }
}
