using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FuzzySoft.ZeroMQMessenger
{
    public static class MessageBusConfig
    {
        public static int PortNumberRouter { get; set; }
        public static int PortNumberDealer { get; set; }
        public static int PortNumberPublisher { get; set; }
        public static string Hostname { get; set; }
    }
}
