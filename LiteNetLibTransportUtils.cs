using System;
using LiteNetLib;
using Mirror;

namespace LiteNetLibMirror
{
    public static class LiteNetLibTransportUtils
    {
        public static DeliveryMethod ConvertChannel(int channel)
        {
            switch (channel)
            {
                case Channels.DefaultReliable:
                    return DeliveryMethod.ReliableOrdered;
                case Channels.DefaultUnreliable:
                    return DeliveryMethod.Unreliable;
                default:
                    throw new ArgumentException("Unexpected channel: " + channel);
            }
        }
    }
}
