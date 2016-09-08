using System;
using System.Threading;
using System.Threading.Tasks;
using ZeroMQ;

namespace FuzzySoft.ZeroMQMessenger
{
    #region TEST EVENTS
    internal delegate void OnRouterMessageReceivedHandler(ZMessage message);
    internal delegate void OnDealerMessageReceivedHandler(ZMessage message);
    internal delegate void OnPublisherMessageReceivedHandler(ZMessage message);
    internal delegate void OnPublisherAckMessageSentHandler(ZFrame frame);
    internal delegate void OnRouterDealerReadyHandler();
    internal delegate void OnPublisherReadyHandler();
    #endregion

    public class ZeroMessenger
    {
        internal event OnRouterMessageReceivedHandler OnRouterMessageReceived;
        internal event OnDealerMessageReceivedHandler OnDealerMessageReceived;
        internal event OnPublisherMessageReceivedHandler OnPublisherMessageReceived;
        internal event OnPublisherAckMessageSentHandler OnPublisherAckMessageSent;
        internal event OnRouterDealerReadyHandler OnRouterDealerReady;
        internal event OnPublisherReadyHandler OnPublisherReady;

        private ZContext context;

        #region Events
        internal void RaiseOnRouterMessageReceived(ZMessage message)
        {
            var handler = OnRouterMessageReceived;
            if (handler == null) return;
            handler(message);
        }

        internal void RaiseOnDealerMessageReceived(ZMessage message)
        {
            var handler = OnDealerMessageReceived;
            if (handler == null) return;
            handler(message);
        }

        internal void RaiseOnPublisherMessageReceived(ZMessage message)
        {
            var handler = OnPublisherMessageReceived;
            if (handler == null) return;
            handler(message);
        }

        internal void RaiseOnPublisherAckMessageSent(ZFrame frame)
        {
            var handler = OnPublisherAckMessageSent;
            if (handler == null) return;
            handler(frame);
        }

        internal void RaiseOnRouterDealerReady()
        {
            var handler = OnRouterDealerReady;
            if (handler == null) return;
            handler();
        }

        internal void RaiseOnPublisherReady()
        {
            var handler = OnPublisherReady;
            if (handler == null) return;
            handler();
        }
        #endregion

        private CancellationTokenSource cts;
        public void Start()
        {
            cts = new CancellationTokenSource();

            context = new ZContext();

            Task.Factory.StartNew(StartRouterDealer, cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            Task.Factory.StartNew(StartPublisher, cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public void StartRouterDealer()
        {
            using (var frontend = new ZSocket(context, ZSocketType.ROUTER))
            using (var backend = new ZSocket(context, ZSocketType.DEALER))
            {
                var frontEndUrl = string.Format("tcp://{0}:{1}", MessageBusConfig.Hostname, MessageBusConfig.PortNumberRouter);
                var backendEndUrl = string.Format("tcp://{0}:{1}", MessageBusConfig.Hostname, MessageBusConfig.PortNumberDealer);

                frontend.Bind(frontEndUrl);
                backend.Bind(backendEndUrl);

                var poll = ZPollItem.CreateReceiver();

                // Switch messages between sockets
                ZError error;
                ZMessage message;

                RaiseOnRouterDealerReady();

                while (true)
                {

                    if (frontend.PollIn(poll, out message, out error, TimeSpan.FromMilliseconds(64)))
                    {
                        RaiseOnRouterMessageReceived(message);
                        // Process all parts of the message
                        backend.Send(message);
                    }
                    else
                    {
                        if (error == ZError.ETERM)
                            return;    // Interrupted
                        if (error != ZError.EAGAIN)
                            throw new ZException(error);
                    }

                    if (backend.PollIn(poll, out message, out error, TimeSpan.FromMilliseconds(64)))
                    {
                        RaiseOnDealerMessageReceived(message);
                        // Process all parts of the message
                        frontend.Send(message);
                    }
                    else
                    {
                        if (error == ZError.ETERM)
                            return;    // Interrupted
                        if (error != ZError.EAGAIN)
                            throw new ZException(error);
                    }
                }
            }
        }

        public void StartPublisher()
        {
            using (var responder = new ZSocket(context, ZSocketType.REP))
            using (var publisher = new ZSocket(context, ZSocketType.PUB))
            {
                var publisherUrl = string.Format("tcp://{0}:{1}", MessageBusConfig.Hostname, MessageBusConfig.PortNumberPublisher);
                var responderUrl = string.Format("tcp://{0}:{1}", MessageBusConfig.Hostname, MessageBusConfig.PortNumberDealer);

                publisher.Bind(publisherUrl);
                responder.Connect(responderUrl);

                RaiseOnPublisherReady();

                while (true)
                {
                    try
                    {
                        // Wait for next request from client
                        using (var message = responder.ReceiveMessage())
                        {
                            var ackFrame = new ZFrame("OK");
                            responder.Send(ackFrame);
                            RaiseOnPublisherAckMessageSent(ackFrame);
                            publisher.Send(message);
                            RaiseOnPublisherMessageReceived(message);
                        }
                    }
                    catch (ZException ex)
                    {
                        if (ex.ErrNo == 156384765)
                        {
                            // context was disposed
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log this error
                    }
                }
            }
        }

        internal void Stop()
        {
            if (context != null)
            {
                context.Dispose();
            }
        }
    }
}