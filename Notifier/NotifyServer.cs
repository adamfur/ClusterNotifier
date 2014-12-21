using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;

namespace Notifier
{
    public class NotifyServer
    {
        private readonly UdpClient _client;
        private IPEndPoint _localEp;

        public NotifyServer()
        {
            _client = new UdpClient();

            _client.ExclusiveAddressUse = false;
            _localEp = new IPEndPoint(IPAddress.Any, 5000);

            _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _client.ExclusiveAddressUse = false;

            _client.Client.Bind(_localEp);

            IPAddress multicastaddress = IPAddress.Parse("239.255.255.19");
            _client.JoinMulticastGroup(multicastaddress);
        }

        public NotifyMessage Read()
        {
            Byte[] data = _client.Receive(ref _localEp);
            string strData = Encoding.Unicode.GetString(data);

            return JsonConvert.DeserializeObject<NotifyMessage>(strData);
        }
    }
}