using System;
namespace MessageReceiverApp.Models
{
    public class ProcessErrorModel
    {
        public int Code { get; set; }
        public string ErrorSource { get; set; }
        public string Namespace { get; set; }
        public string EntityPath { get; set; }
        public string Message { get; set; }
    }
}
