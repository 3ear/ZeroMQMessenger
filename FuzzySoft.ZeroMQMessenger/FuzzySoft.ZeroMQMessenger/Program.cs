using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topshelf;

namespace FuzzySoft.ZeroMQMessenger
{
    class Program
    {
        public static string ServiceName = "ZeroMQ Messenger";

        static void Main(string[] args)
        {
            HostFactory.Run(x =>
            {
                x.Service<MessageBusService>(s =>
                {
                    s.ConstructUsing(name => new MessageBusService());
                    s.WhenStarted(ss => ss.Start());
                    s.WhenStopped(ss => ss.Stop());
                });

                x.StartAutomatically();
                x.RunAsLocalSystem();
                x.SetDescription("FuzzySoft " + ServiceName);
                x.SetDisplayName("FuzzySoft " + ServiceName);
                x.SetServiceName("FuzzySoft." + ServiceName.Replace(" ", "").ToUpper());
            });
        }
    }
}
