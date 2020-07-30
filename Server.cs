using System;
using System.Collections.Generic;
using System.Net;
using LiteNetLib;
using Mirror;
using UnityEngine;

namespace LiteNetLibMirror
{
    public delegate void OnConnected(int clientId);
    public delegate void OnServerData(int clientId, ArraySegment<byte> data);
    public delegate void OnDisconnected(int clientId);

    public class Server
    {
        private const string Scheme = "litenet";
        private const int ConnectionCapacity = 1000;

        // configuration
        readonly ushort port;
        readonly int updateTime;
        readonly int disconnectTimeout;
        readonly ILogger logger;

        // LiteNetLib state
        NetManager server;
        Dictionary<int, NetPeer> connections = new Dictionary<int, NetPeer>(ConnectionCapacity);

        public event OnConnected onConnected;
        public event OnServerData onData;
        public event OnDisconnected onDisconnected;

        public Server(ushort port, int updateTime, int disconnectTimeout, ILogger logger)
        {
            this.port = port;
            this.updateTime = updateTime;
            this.disconnectTimeout = disconnectTimeout;
            this.logger = logger;
        }


        public void Start()
        {
            // not if already started
            if (server != null)
            {
                logger.LogWarning("LiteNetLib: server already started.");
                return;
            }

            logger.Log("LiteNet SV: starting...");

            // create server
            EventBasedNetListener listener = new EventBasedNetListener();
            server = new NetManager(listener);
            server.UpdateTime = updateTime;
            server.DisconnectTimeout = disconnectTimeout;

            // set up events
            listener.ConnectionRequestEvent += Listener_ConnectionRequestEvent;
            listener.PeerConnectedEvent += Listener_PeerConnectedEvent;
            listener.NetworkReceiveEvent += Listener_NetworkReceiveEvent;
            listener.PeerDisconnectedEvent += Listener_PeerDisconnectedEvent;
            listener.NetworkErrorEvent += Listener_NetworkErrorEvent;


            // start listening
            server.Start(port);
        }

        private void Listener_ConnectionRequestEvent(ConnectionRequest request)
        {
            logger.Log("LiteNet SV connection request");
            request.AcceptIfKey(LiteNetLibTransportUtils.ConnectKey);
        }

        private void Listener_PeerConnectedEvent(NetPeer peer)
        {
            if (logger.LogEnabled()) logger.Log($"LiteNet SV client connected: {peer.EndPoint} id={peer.Id}");
            connections[peer.Id] = peer;
            onConnected?.Invoke(peer.Id);
        }

        private void Listener_NetworkReceiveEvent(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            if (logger.LogEnabled()) logger.Log($"LiteNet SV received {reader.AvailableBytes} bytes. method={deliveryMethod}");
            onData?.Invoke(peer.Id, reader.GetRemainingBytesSegment());
            reader.Recycle();
        }

        private void Listener_PeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            // this is called both when a client disconnects, and when we
            // disconnect a client.
            if (logger.LogEnabled()) logger.Log($"LiteNet SV client disconnected: {peer.EndPoint} info={disconnectInfo}");
            onDisconnected?.Invoke(peer.Id);
            connections.Remove(peer.Id);
        }

        private void Listener_NetworkErrorEvent(IPEndPoint endPoint, System.Net.Sockets.SocketError socketError)
        {
            if (logger.WarnEnabled()) logger.LogWarning($"LiteNet SV network error: {endPoint} error={socketError}");
            // TODO should we disconnect or is it called automatically?
        }

        public void Stop()
        {
            if (server != null)
            {
                server.Stop();
                server = null;
            }
        }


        public bool Send(List<int> connectionIds, int channelId, ArraySegment<byte> segment)
        {
            if (server != null)
            {
                bool success = true;
                foreach (int connectionId in connectionIds)
                {
                    success &= SendOne(connectionId, channelId, segment);
                }
            }
            logger.LogWarning("LiteNet SV: can't send because not started yet.");
            return false;
        }

        private bool SendOne(int connectionId, int channelId, ArraySegment<byte> segment)
        {
            if (connections.TryGetValue(connectionId, out NetPeer peer))
            {
                try
                {
                    // convert Mirror channel to LiteNetLib channel & send
                    DeliveryMethod deliveryMethod = LiteNetLibTransportUtils.ConvertChannel(channelId);
                    peer.Send(segment.Array, segment.Offset, segment.Count, deliveryMethod);
                    return true;
                }
                catch (TooBigPacketException exception)
                {
                    if (logger.WarnEnabled()) logger.LogWarning($"LiteNet SV: send failed for connectionId={connectionId} reason={exception}");
                    return false;
                }
            }
            if (logger.WarnEnabled()) logger.LogWarning($"LiteNet SV: invalid connectionId={connectionId}");
            return false;
        }

        /// <summary>
        /// Kicks player
        /// </summary>
        /// <param name="connectionId"></param>
        /// <returns></returns>
        public bool Disconnect(int connectionId)
        {
            if (server != null)
            {
                if (connections.TryGetValue(connectionId, out NetPeer peer))
                {
                    // disconnect the client.
                    // PeerDisconnectedEvent will call OnDisconnect.
                    peer.Disconnect();
                    return true;
                }
                if (logger.WarnEnabled()) logger.LogWarning($"LiteNet SV: invalid connectionId={connectionId}");
                return false;
            }
            return false;
        }

        public Uri GetUri()
        {
            UriBuilder builder = new UriBuilder();
            builder.Scheme = Scheme;
            builder.Host = Dns.GetHostName();
            builder.Port = port;
            return builder.Uri;
        }

        public string GetClientAddress(int connectionId)
        {
            if (server != null)
            {
                if (connections.TryGetValue(connectionId, out NetPeer peer))
                {
                    return peer.EndPoint.Address.ToString();
                }
            }
            return "";
        }

        public void OnUpdate()
        {
            if (server != null)
            {
                server.PollEvents();
            }
        }
    }
}
