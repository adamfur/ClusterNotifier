using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;

namespace Notifier
{
    public interface INotifyClient
    {
        void AttemptToBecomeMaster();
        void Heartbeat();
        void Broadcast();
    }

    public class NotifyClient : INotifyClient
    {
        private readonly Guid _applicationId;
        private readonly Guid _applicationInstanceId;
        private readonly int _started;
        private readonly UdpClient _udpclient;
        private readonly IPEndPoint _remoteep;

        public NotifyClient(Guid argApplicationId, Guid argApplicationInstanceId, int argStarted)
        {
            _udpclient = new UdpClient();

            IPAddress multicastaddress = IPAddress.Parse("239.255.255.19");
            _udpclient.JoinMulticastGroup(multicastaddress);
            _remoteep = new IPEndPoint(multicastaddress, 5000);
            _applicationId = argApplicationId;
            _applicationInstanceId = argApplicationInstanceId;
            _started = argStarted;
        }

        private void Send(NotifyMessage message)
        {
            Byte[] buffer = Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(message));
            _udpclient.Send(buffer, buffer.Length, _remoteep);
        }

        public void AttemptToBecomeMaster()
        {
            var message = CreateMessage(EventType.PromoteSelf);

            Send(message);
            Console.WriteLine("***AttemptToBecomeMaster***");
        }

        public void Heartbeat()
        {
            var message = CreateMessage(EventType.Heartbeat);

            Send(message);
            Console.WriteLine("***Heartbeat***");
        }

        public void Broadcast()
        {
            var message = CreateMessage(EventType.Notify);

            Send(message);
            Console.WriteLine("***Broadcast***");
        }

        private NotifyMessage CreateMessage(EventType argEventType)
        {
            return new NotifyMessage
            {
                ApplicationId = _applicationId,
                ApplicationInstanceId = _applicationInstanceId,
                Type = argEventType,
                Started = _started
            };
        }
    }
}