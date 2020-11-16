using System;
using System.Net;
using LiteNetLib;
using Mirror;
using UnityEngine;

namespace LiteNetLibMirror
{
    public delegate void OnClientData(ArraySegment<byte> data, int channel);

    public class Client
    {

        // configuration
        readonly ushort port;
        readonly int updateTime;
        readonly int disconnectTimeout;
        readonly ILogger logger;

        // LiteNetLib state
        NetManager client;

        public event Action onConnected;
        public event OnClientData onData;
        public event Action onDisconnected;

        public IPEndPoint RemoteEndPoint => client.FirstPeer.EndPoint;

        public Client(ushort port, int updateTime, int disconnectTimeout, ILogger logger)
        {
            this.port = port;
            this.updateTime = updateTime;
            this.disconnectTimeout = disconnectTimeout;
            this.logger = logger;
        }

        public bool Connected { get; private set; }

        public void Connect(string address, int maxConnectAttempts, bool ipv6Enabled)
        {
            // not if already connected or connecting
            if (client != null)
            {
                logger.LogWarning("LiteNet: client already connected/connecting.");
                return;
            }

            logger.Log("LiteNet CL: connecting...");

            // create client
            EventBasedNetListener listener = new EventBasedNetListener();
            client = new NetManager(listener);
            client.UpdateTime = updateTime;
            client.DisconnectTimeout = disconnectTimeout;
            client.MaxConnectAttempts = maxConnectAttempts;

            // DualMode seems to break some addresses, so make this an option so that it can be turned on when needed
            if (ipv6Enabled)
            {
                client.IPv6Enabled = IPv6Mode.DualMode;
            }

            // set up events
            listener.PeerConnectedEvent += Listener_PeerConnectedEvent;
            listener.NetworkReceiveEvent += Listener_NetworkReceiveEvent;
            listener.PeerDisconnectedEvent += Listener_PeerDisconnectedEvent;
            listener.NetworkErrorEvent += Listener_NetworkErrorEvent;

            // start & connect
            client.Start();
            client.Connect(address, port, LiteNetLibTransportUtils.ConnectKey);
        }

        private void Listener_PeerConnectedEvent(NetPeer peer)
        {
            if (logger.LogEnabled()) logger.Log($"LiteNet CL client connected: {peer.EndPoint}");
            Connected = true;
            onConnected?.Invoke();
        }

        private void Listener_NetworkReceiveEvent(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            if (logger.LogEnabled()) logger.Log($"LiteNet CL received {reader.AvailableBytes} bytes. method={deliveryMethod}");
            int mirrorChannel = LiteNetLibTransportUtils.ConvertChannel(deliveryMethod);
            onData?.Invoke(reader.GetRemainingBytesSegment(), mirrorChannel);
            reader.Recycle();
        }

        private void Listener_PeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            // this is called when the server stopped.
            // this is not called when the client disconnected.
            if (logger.LogEnabled()) logger.Log($"LiteNet CL disconnected. info={disconnectInfo}");
            Connected = false;
            Disconnect();
        }

        private void Listener_NetworkErrorEvent(System.Net.IPEndPoint endPoint, System.Net.Sockets.SocketError socketError)
        {
            if (logger.WarnEnabled()) logger.LogWarning($"LiteNet CL network error: {endPoint} error={socketError}");
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
                onDisconnected?.Invoke();
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
                    if (logger.WarnEnabled()) logger.LogWarning($"LiteNet CL: send failed. reason={exception}");
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
