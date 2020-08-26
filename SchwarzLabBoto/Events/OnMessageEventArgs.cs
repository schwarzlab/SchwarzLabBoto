using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchwarzLab.Clients.Events
{
    public class OnMessageEventArgs : EventArgs
    {
        public string Message { get; set; }
    }
}
