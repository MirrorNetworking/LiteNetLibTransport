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

        Client client;
        Server server;

        void Awake()
        {
            // tell Telepathy to use Unity's Debug.Log
            LiteNetLibMirror.Logger.Log = logger.Log;
            LiteNetLibMirror.Logger.LogWarning = logger.LogWarning;
            LiteNetLibMirror.Logger.LogError = logger.LogError;

            Debug.Log("LiteNetLibMirrorTransport initialized!");
        }

        public override void Shutdown()
        {
            throw new NotImplementedException();
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



        #region CLIENT
        public override bool ClientConnected()
        {
            return client != null && client.Connected;
        }

        public override void ClientConnect(string address)
        {
            if (!ClientConnected())
            {
                client = new Client();
                client.Connect(address);
            }
            else
            {
                Debug.LogWarning("Can't start client as one was already connected");
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
                Debug.LogWarning("Can't stop client as no client was connected");
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
                server = new Server();
                server.Start();
            }
            else
            {
                Debug.LogWarning("Can't start server as one was already active");
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
                Debug.LogWarning("Can't stop server as no server was active");
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
