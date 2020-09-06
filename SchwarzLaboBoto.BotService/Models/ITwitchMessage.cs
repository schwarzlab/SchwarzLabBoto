using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Text;

namespace SchwarzLaboBoto.BotService.Models
{
    public interface ITwitchMessage
    {
        public TwtichMessageTypes MessageType { get; set; }
        public string Message { get; set; }
        public string Username { get; set; }
        public string MessageId { get; set; }
    }
}
