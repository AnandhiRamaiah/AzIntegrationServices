using System;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace MessageReceiverApp.Models
{
    public class HeaderModel
    {

        [FromHeader(Name ="ConnectionString")]
        public string ConnectionString { get; set; }       

    }
}
