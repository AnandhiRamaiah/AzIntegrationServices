using System;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace TweetProocessorApp.Models
{
    public class MessageModel
    {

        [JsonProperty("name")]        
        public string Name { get; set; }

        [JsonProperty("tweet")]
        public string Tweet { get; set; }


    }
}
