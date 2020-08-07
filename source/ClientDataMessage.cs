using System;

namespace Mirror
{
    struct ClientDataMessage
    {
        public ArraySegment<byte> data;
        public int channel;

        public ClientDataMessage(ArraySegment<byte> data, int channel)
        {
            this.data = data;
            this.channel = channel;
        }
    }
}
