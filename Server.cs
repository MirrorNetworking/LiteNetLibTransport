using System;
using System.Collections.Generic;
using LiteNetLib;

namespace LiteNetLibMirror
{
    public class Server
    {
        // configuration
        ushort Port = 8888;
        int UpdateTime = 15;
        int DisconnectTimeout = 5000;


        // LiteNetLib state
        NetManager server;
        Dictionary<int, NetPeer> connections = new Dictionary<int, NetPeer>(1000);

        /// <summary>
        /// Kicks player
        /// </summary>
        /// <param name="connectionId"></param>
        /// <returns></returns>
        internal bool Disconnect(int connectionId)
        {
            throw new NotImplementedException();
        }

        internal void Stop()
        {
            throw new NotImplementedException();
        }

        internal void Start()
        {
            throw new NotImplementedException();
        }

        internal bool Send(List<int> connectionIds, int channelId, ArraySegment<byte> segment)
        {
            throw new NotImplementedException();
        }

        internal Uri GetUri()
        {
            throw new NotImplementedException();
        }

        internal string GetClientAddress(int connectionId)
        {
            throw new NotImplementedException();
        }
    }
}
