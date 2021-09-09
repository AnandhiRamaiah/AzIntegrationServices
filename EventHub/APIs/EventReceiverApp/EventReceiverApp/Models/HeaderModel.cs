using System;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace EventReceiverApp.Models
{
    public class HeaderModel
    {

        [FromHeader(Name ="ConnectionString")]
        public string ConnectionString { get; set; }

    }
}
