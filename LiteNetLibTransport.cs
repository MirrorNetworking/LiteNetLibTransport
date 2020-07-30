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

        Client client;
        Server server;

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
            if (ClientConnected())
            {
                client.OnUpdate();
            }
            if (ServerActive())
            {
                server.OnUpdate();
            }
        }

        #region CLIENT
        public override bool ClientConnected()
        {
            return client != null && client.Connected;
        }

        public override void ClientConnect(string address)
        {
            if (!ClientConnected())
            {
                client = new Client(port, updateTime, disconnectTimeout, logger);

                client.onConnected += () => OnClientConnected.Invoke();
                client.onData += data => OnClientDataReceived.Invoke(data, Channels.DefaultReliable);
                client.onDisconnected += () => OnClientDisconnected.Invoke();

                client.Connect(address);
            }
            else
            {
                logger.LogWarning("Can't start client as one was already connected");
            }
        }

        public override void ClientDisconnect()
        {
            if (ClientConnected())
            {
                client.Disconnect();
                client = null;
            }
            else
            {
                logger.LogWarning("Can't stop client as no client was connected");
            }
        }

        public override bool ClientSend(int channelId, ArraySegment<byte> segment)
        {
            return client.Send(channelId, segment);
        }
        #endregion


        #region SERVER
        public override bool ServerActive()
        {
            return server != null;
        }

        public override void ServerStart()
        {
            if (!ServerActive())
            {
                server = new Server(port, updateTime, disconnectTimeout, logger);

                server.onConnected += (clientId) => OnServerConnected.Invoke(clientId);
                server.onData += (clientId, data) => OnServerDataReceived.Invoke(clientId, data, Channels.DefaultReliable);
                server.onDisconnected += (clientId) => OnServerDisconnected.Invoke(clientId);

                server.Start();
            }
            else
            {
                logger.LogWarning("Can't start server as one was already active");
            }
        }

        public override void ServerStop()
        {
            if (ServerActive())
            {
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
            return server.Send(connectionIds, channelId, segment);
        }

        public override bool ServerDisconnect(int connectionId)
        {
            return server.Disconnect(connectionId);
        }

        public override string ServerGetClientAddress(int connectionId)
        {
            return server.GetClientAddress(connectionId);
        }

        public override Uri ServerUri()
        {
            return server.GetUri();
        }
        #endregion
    }
}
