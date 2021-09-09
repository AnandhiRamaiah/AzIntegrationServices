using System;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;


namespace MessageReceiverApp.Models
{
    public class ForwardHeaderModel : HeaderModel
    {

        [FromHeader(Name = "NextHopConnectionString")]
        public string NextHopConnectionString { get; set; }

        [FromHeader(Name = "NextHopTopic")]
        public string NextHopTopicName { get; set; }        

        [FromHeader(Name = "NextHopSession")]
        public string NextHopSessionName { get; set; }


    }
}
