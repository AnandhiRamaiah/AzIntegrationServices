using System;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;


namespace MessageProducerApp.Models
{
    public class ForwardHeaderModel : HeaderModel
    {        

        [FromHeader(Name = "NextHopQueueConnectionString")]
        public string NextHopQueueConnectionString { get; set; }

        [FromHeader(Name = "NextHopTopic")]
        public string NextHopQueueName { get; set; }


    }
}
