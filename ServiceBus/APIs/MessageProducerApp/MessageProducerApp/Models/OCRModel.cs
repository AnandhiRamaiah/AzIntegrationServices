using System;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace MessageProducerApp.Models
{
    public class OCRModel
    {

        [JsonProperty("ocrId")]        
        public string OCRId { get; set; }

        [JsonProperty("domain")]
        public string Domain { get; set; }

        [JsonProperty("name")]        
        public string Name { get; set; }

        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }


    }
}
