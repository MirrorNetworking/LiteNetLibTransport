using System;
using LiteNetLib;

namespace LiteNetLibMirror
{
    public class Client
    {
        private const string ConnectKey = "MIRROR_LITENETLIB";

        // configuration
        readonly ushort port;
        readonly int updateTime;
        readonly int disconnectTimeout;

        // LiteNetLib state
        NetManager client;

        public event Action OnConnected;
        public event Action<ArraySegment<byte>> OnData;
        public event Action OnDisconnected;

        public Client(ushort port, int updateTime, int disconnectTimeout)
        {
            this.port = port;
            this.updateTime = updateTime;
            this.disconnectTimeout = disconnectTimeout;
        }

        public bool Connected { get; private set; }

        public void Connect(string address)
        {
            // not if already connected or connecting
            if (client != null)
            {
                Logger.LogWarning("LiteNet: client already connected/connecting.");
                return;
            }

            Logger.Log("LiteNet CL: connecting...");

            // create client
            EventBasedNetListener listener = new EventBasedNetListener();
            client = new NetManager(listener);
            client.UpdateTime = updateTime;
            client.DisconnectTimeout = disconnectTimeout;

            // set up events
            listener.PeerConnectedEvent += Listener_PeerConnectedEvent;
            listener.NetworkReceiveEvent += Listener_NetworkReceiveEvent;
            listener.PeerDisconnectedEvent += Listener_PeerDisconnectedEvent;
            listener.NetworkErrorEvent += Listener_NetworkErrorEvent;

            // start & connect
            client.Start();
            client.Connect(address, port, ConnectKey);
        }

        private void Listener_PeerConnectedEvent(NetPeer peer)
        {
            Logger.Log("LiteNet CL client connected: " + peer.EndPoint);
            Connected = true;
            OnConnected?.Invoke();
        }

        private void Listener_NetworkReceiveEvent(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            Logger.Log("LiteNet CL received " + reader.AvailableBytes + " bytes. method=" + deliveryMethod);
            OnData?.Invoke(reader.GetRemainingBytesSegment());
            reader.Recycle();
        }

        private void Listener_PeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            // this is called when the server stopped.
            // this is not called when the client disconnected.
            Logger.Log("LiteNet CL disconnected. info=" + disconnectInfo);
            Connected = false;
            Disconnect();
        }

        private void Listener_NetworkErrorEvent(System.Net.IPEndPoint endPoint, System.Net.Sockets.SocketError socketError)
        {
            Logger.LogWarning("LiteNet CL network error: " + endPoint + " error=" + socketError);
            // TODO should we disconnect or is it called automatically?
        }


        public void Disconnect()
        {
            if (client != null)
            {
                // clean up
                client.Stop();
                client = null;
                Connected = false;

                // PeerDisconnectedEvent is not called when voluntarily
                // disconnecting. need to call OnDisconnected manually.
                OnDisconnected?.Invoke();
            }
        }

        public bool Send(int channelId, ArraySegment<byte> segment)
        {
            if (client != null && client.FirstPeer != null)
            {
                try
                {
                    // convert Mirror channel to LiteNetLib channel & send
                    DeliveryMethod deliveryMethod = LiteNetLibTransportUtils.ConvertChannel(channelId);
                    client.FirstPeer.Send(segment.Array, segment.Offset, segment.Count, deliveryMethod);
                    return true;
                }
                catch (TooBigPacketException exception)
                {
                    Logger.LogWarning("LiteNet CL: send failed. reason=" + exception);
                    return false;
                }
            }
            return false;
        }

        public void OnUpdate()
        {
            // only if connected or connecting
            if (client != null)
            {
                client.PollEvents();
            }
        }
    }
}
