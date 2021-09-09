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
        public static async Task Run([ServiceBusTrigger("tweetqueue",
                                        Connection = "ServiceBusQueueConnection")]
                                        Message message,
                                        MessageReceiver messageReceiver,
                                        Int32 deliveryCount,
                                        string messageId,
                                        ILogger log)
        {    
          
            log.LogInformation($"messageId:{messageId}");
            log.LogInformation($"deliveryCount:{deliveryCount}");

            var messageItem = JsonConvert.DeserializeObject<MessageModel>
                              (Encoding.UTF7.GetString(message.Body));

            await messageReceiver.DeadLetterAsync(message.SystemProperties.LockToken);
            //await messageReceiver.CompleteAsync(message.SystemProperties.LockToken);            

            log.LogInformation($"Name:{messageItem.Name}");
            log.LogInformation($"Tweet:{messageItem.Tweet}");            

        }

        //[FunctionName("TweetProocessorApp")]
        //public static async Task Run([ServiceBusTrigger("tweetqueue",
        //                        Connection = "ServiceBusQueueConnection")]
        //                        Message message,        
        //                        Int32 deliveryCount,
        //                        string messageId,
        //                        ILogger log)
        //{

        //    log.LogInformation($"messageId:{messageId}");
        //    log.LogInformation($"deliveryCount:{deliveryCount}");

        //    var messageItem = JsonConvert.DeserializeObject<MessageModel>
        //                      (Encoding.UTF7.GetString(message.Body));                    

        //    log.LogInformation($"Name:{messageItem.Name}");
        //    log.LogInformation($"Tweet:{messageItem.Tweet}");

        //}
    }
}
