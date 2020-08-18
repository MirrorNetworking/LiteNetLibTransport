using System;
using System.Collections.Generic;
using LiteNetLib;
using LiteNetLibMirror;

using UnityEngine;

namespace Mirror
{
    public class LiteNetLibTransport : Transport
    {
        static readonly ILogger logger = LogFactory.GetLogger<LiteNetLibTransport>();

        [Header("Config")]
        public ushort port = 8888;
        public int updateTime = 15;
        public int disconnectTimeout = 5000;

        /// <summary>
        /// Active Client, null is no client is active
        /// </summary>
        Client client;
        /// <summary>
        /// Active Server, null is no Server is active
        /// </summary>
        Server server;

        /// <summary>
        /// Client message recieved while Transport was disabled
        /// </summary>
        readonly Queue<ClientDataMessage> clientDisabledQueue = new Queue<ClientDataMessage>();

        /// <summary>
        /// Server message recieved while Transport was disabled
        /// </summary>
        readonly Queue<ServerDataMessage> serverDisabledQueue = new Queue<ServerDataMessage>();
        /// <summary>
        /// If messages were added to DisabledQueues
        /// </summary>
        bool checkMessageQueues;

        void Awake()
        {
            logger.Log("LiteNetLibTransport initialized!");
        }

        public override void Shutdown()
        {
            logger.Log("LiteNetLibTransport Shutdown");
            client?.Disconnect();
            server?.Stop();
        }

        public override bool Available()
        {
            // all except WebGL
            return Application.platform != RuntimePlatform.WebGLPlayer;
        }

        public override int GetMaxPacketSize(int channelId = 0)
        {
            // LiteNetLib NetPeer construct calls SetMTU(0), which sets it to
            // NetConstants.PossibleMtu[0] which is 576-68.
            // (bigger values will cause TooBigPacketException even on loopback)
            //
            // see also: https://github.com/RevenantX/LiteNetLib/issues/388
            return NetConstants.PossibleMtu[0];
        }

        private void LateUpdate()
        {
            // check for messages in queue before processing new messages
            if (enabled && checkMessageQueues)
            {
                while (enabled && clientDisabledQueue.Count > 0)
                {
                    ClientDataMessage data = clientDisabledQueue.Dequeue();
                    OnClientDataReceived.Invoke(data.data, data.channel);
                }
                while (enabled && serverDisabledQueue.Count > 0)
                {
                    ServerDataMessage data = serverDisabledQueue.Dequeue();
                    OnServerDataReceived.Invoke(data.clientId, data.data, data.channel);
                }

                checkMessageQueues = false;
            }

            if (client != null)
            {
                client.OnUpdate();
            }
            if (server != null)
            {
                server.OnUpdate();
            }
        }

        public override string ToString()
        {
            if (server != null)
            {
                // printing server.listener.LocalEndpoint causes an Exception
                // in UWP + Unity 2019:
                //   Exception thrown at 0x00007FF9755DA388 in UWF.exe:
                //   Microsoft C++ exception: Il2CppExceptionWrapper at memory
                //   location 0x000000E15A0FCDD0. SocketException: An address
                //   incompatible with the requested protocol was used at
                //   System.Net.Sockets.Socket.get_LocalEndPoint ()
                // so let's use the regular port instead.
                return "TeleLiteNetLibpathy Server port: " + port;
            }
            else if (client != null)
            {
                if (client.Connected)
                {
                    return "LiteNetLib Client ip: " + client.RemoteEndPoint;
                }
                else
                {
                    return "LiteNetLib Connecting...";
                }
            }
            return "LiteNetLib (inactive/disconnected)";
        }

        #region CLIENT
        public override bool ClientConnected() => client != null && client.Connected;

        public override void ClientConnect(string address)
        {
            if (client != null)
            {
                logger.LogWarning("Can't start client as one was already connected");
                return;
            }

            client = new Client(port, updateTime, disconnectTimeout, logger);

            client.onConnected += OnClientConnected.Invoke;
            client.onData += Client_onData;
            client.onDisconnected += OnClientDisconnected.Invoke;

            client.Connect(address);
        }

        private void Client_onData(ArraySegment<byte> data, int channel)
        {
            if (enabled)
            {
                OnClientDataReceived.Invoke(data, channel);
            }
            else
            {
                clientDisabledQueue.Enqueue(new ClientDataMessage(data, channel));
                checkMessageQueues = true;
            }
        }

        public override void ClientDisconnect()
        {
            if (client != null)
            {
                // remove events before calling disconnect so stop loops within mirror
                client.onConnected -= OnClientConnected.Invoke;
                client.onData -= OnClientDataReceived.Invoke;
                client.onDisconnected -= OnClientDisconnected.Invoke;

                client.Disconnect();
                client = null;
            }
        }

        public override bool ClientSend(int channelId, ArraySegment<byte> segment)
        {
            if (client == null || !client.Connected)
            {
                logger.LogWarning("Can't send when client is not connected");
                return false;
            }
            return client.Send(channelId, segment);
        }
        #endregion


        #region SERVER
        public override bool ServerActive() => server != null;

        public override void ServerStart()
        {
            if (server != null)
            {
                logger.LogWarning("Can't start server as one was already active");
                return;
            }

            server = new Server(port, updateTime, disconnectTimeout, logger);

            server.onConnected += OnServerConnected.Invoke;
            server.onData += Server_onData;
            server.onDisconnected += OnServerDisconnected.Invoke;

            server.Start();
        }

        private void Server_onData(int clientId, ArraySegment<byte> data, int channel)
        {
            if (enabled)
            {
                OnServerDataReceived.Invoke(clientId, data, channel);
            }
            else
            {
                serverDisabledQueue.Enqueue(new ServerDataMessage(clientId, data, channel));
                checkMessageQueues = true;
            }
        }

        public override void ServerStop()
        {
            if (server != null)
            {
                server.onConnected -= OnServerConnected.Invoke;
                server.onData -= OnServerDataReceived.Invoke;
                server.onDisconnected -= OnServerDisconnected.Invoke;

                server.Stop();
                server = null;
            }
            else
            {
                logger.LogWarning("Can't stop server as no server was active");
            }
        }

        public override bool ServerSend(List<int> connectionIds, int channelId, ArraySegment<byte> segment)
        {
            if (server == null)
            {
                logger.LogWarning("Can't send when Server is not active");
                return false;
            }

            return server.Send(connectionIds, channelId, segment);
        }

        public override bool ServerDisconnect(int connectionId)
        {
            if (server == null)
            {
                logger.LogWarning("Can't disconnect when Server is not active");
                return false;
            }

            return server.Disconnect(connectionId);
        }

        public override string ServerGetClientAddress(int connectionId)
        {
            return server?.GetClientAddress(connectionId);
        }

        public override Uri ServerUri()
        {
            return server?.GetUri();
        }
        #endregion
    }
}
