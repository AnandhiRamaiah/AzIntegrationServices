using System;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace MessageProducerApp.Models
{
    public class MessageModel
    {

        [JsonProperty("messageId")]        
        public string MessageId { get; set; }

        [JsonProperty("name")]        
        public string Name { get; set; }

        [JsonProperty("tweet")]
        public string Tweet { get; set; }


    }
}
