using System;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace EventGridProducerApp.Models
{
    public class HeaderModel
    {

        [FromHeader(Name ="AccessKey")]
        public string AccessKeyString { get; set; }

        [FromHeader(Name = "TopicEndpoint")]
        public string TopicEndpointString { get; set; }


    }
}
