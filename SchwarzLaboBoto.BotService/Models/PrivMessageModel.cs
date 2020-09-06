using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace SchwarzLaboBoto.BotService.Models
{
    public class PrivMessageModel : ITwitchMessage
    {
        //todo: figure out how to reference the props that have -
        //as normal prop like badge-info to BadgeInfo
        [DisplayName("badge-info")]
        public string BadgeInfo { get; set; } = string.Empty;
        public string Username { get; set; }
        public string MessageId { get; set; }
        public string Message { get; set; }
        public TwtichMessageTypes MessageType { get; set; }
   
    }
}
