using System;
using System.Configuration;

namespace FuzzySoft.ZeroMQMessenger
{
    public class MessageBusService
    {
        private ZeroMessenger messenger;

        public MessageBusService()
        {
            MessageBusConfig.Hostname = ConfigurationManager.AppSettings["Hostname"];
            MessageBusConfig.PortNumberDealer = Int32.Parse(ConfigurationManager.AppSettings["PortNumberDealer"]);
            MessageBusConfig.PortNumberPublisher = Int32.Parse(ConfigurationManager.AppSettings["PortNumberPublisher"]);
            MessageBusConfig.PortNumberRouter = Int32.Parse(ConfigurationManager.AppSettings["PortNumberRouter"]);
        }

        public void Start()
        {
            messenger = new ZeroMessenger();
            messenger.Start();
        }

        public void Stop()
        {
            messenger.Stop();
        }
    }
}
