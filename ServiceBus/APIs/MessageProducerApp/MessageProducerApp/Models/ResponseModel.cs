using System;
namespace MessageProducerApp.Models
{
    public class ResponseModel
    {


        private string Message { get; set; }
        private int Code { get; set; }

        public ResponseModel(string msg, int code)
        {

            Message = msg;
            Code = code;

        }


    }
}
