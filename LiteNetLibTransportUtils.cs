using System;
using LiteNetLib;
using Mirror;

namespace LiteNetLibMirror
{
    public static class LiteNetLibTransportUtils
    {
        public const string ConnectKey = "MIRROR_LITENETLIB";

        /// <summary>
        /// convert Mirror channel to LiteNetLib channel
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
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
