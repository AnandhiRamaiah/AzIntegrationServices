using System;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace MessageReceiverApp.Models
{
    public class MessageModel
    {

        [JsonProperty("messageId")]        
        public string MessageId { get; set; }

        [JsonProperty("name")]        
        public string Name { get; set; }

        [JsonProperty("tweet")]
        public string Tweet { get; set; }

        public long SequenceNumber { get; set; }


    }
}
