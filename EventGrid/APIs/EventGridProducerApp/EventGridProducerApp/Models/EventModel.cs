using System;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace EventGridProducerApp.Models
{
    public class EventModel
    {

        [JsonProperty("deviceId")]
        public string DeviceId { get; set; }

        [JsonProperty("name")]        
        public string Name { get; set; }

        [JsonProperty("trigger")]
        public string Trigger { get; set; }


    }
}
