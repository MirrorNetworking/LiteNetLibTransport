using System;

namespace Mirror
{
    struct ServerDataMessage
    {
        public int clientId;
        public ArraySegment<byte> data;
        public int channel;

        public ServerDataMessage(int clientId, ArraySegment<byte> data, int channel)
        {
            this.clientId = clientId;
            this.data = data;
            this.channel = channel;
        }
    }
}
