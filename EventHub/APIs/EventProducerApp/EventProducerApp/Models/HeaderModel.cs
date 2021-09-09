using System;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace EventProducerApp.Models
{
    public class HeaderModel
    {

        [FromHeader(Name ="ConnectionString")]
        public string ConnectionString { get; set; }

        [FromHeader(Name = "MaxSizeInMB")]
        public Double MaxSizeInMB { get; set; }

        [FromHeader(Name = "PartitionId")]
        public string PartitionId { get; set; }

        [FromHeader(Name = "PartitionKey")]
        public string PartitionKey { get; set; }


    }
}
