using System;
using LiteNetLib;
using Mirror;

namespace Mirror
{
    public class LiteNetLibChannels
    {
        // channels have an offset of +10 unless they are the default channel that mirror has built in

        /// <summary>
        /// Unreliable. Packets can be dropped, can be duplicated, can arrive without order.
        /// <para>DefaultUnreliable is Unreliable</para>
        /// </summary>
        public const int Unreliable = 1;

        /// <summary>
        /// Reliable. Packets won't be dropped, won't be duplicated, can arrive without order.
        /// </summary>
        public const int ReliableUnordered = 10;

        /// <summary>
        /// Unreliable. Packets can be dropped, won't be duplicated, will arrive in order.
        /// </summary>
        public const int Sequenced = 11;

        /// <summary>
        /// Reliable and ordered. Packets won't be dropped, won't be duplicated, will arrive in order.
        /// <para>DefaultReliable is ReliableOrdered</para>
        /// </summary>
        public const int ReliableOrdered = 0;

        /// <summary>
        /// Reliable only last packet. Packets can be dropped (except the last one), won't be duplicated, will arrive in order.
        /// </summary>
        public const int ReliableSequenced = 13;
    }
}
namespace LiteNetLibMirror
{
    public static class LiteNetLibTransportUtils
    {
        public static string ConnectKey => "MIRROR_LITENETLIB";

        /// <summary>
        /// convert Mirror channel to LiteNetLib channel
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        public static DeliveryMethod ConvertChannel(int channel)
        {
            switch (channel)
            {
                case LiteNetLibChannels.ReliableOrdered:
                    return DeliveryMethod.ReliableOrdered;
                case LiteNetLibChannels.Unreliable:
                    return DeliveryMethod.Unreliable;
                case LiteNetLibChannels.ReliableUnordered:
                    return DeliveryMethod.ReliableUnordered;
                case LiteNetLibChannels.Sequenced:
                    return DeliveryMethod.Sequenced;
                case LiteNetLibChannels.ReliableSequenced:
                    return DeliveryMethod.ReliableSequenced;
                default:
                    throw new ArgumentException("Unexpected channel: " + channel);
            }
        }

        public static int ConvertChannel(DeliveryMethod channel)
        {
            switch (channel)
            {
                case DeliveryMethod.ReliableOrdered:
                    return LiteNetLibChannels.ReliableOrdered;
                case DeliveryMethod.Unreliable:
                    return LiteNetLibChannels.Unreliable;
                case DeliveryMethod.ReliableUnordered:
                    return LiteNetLibChannels.ReliableUnordered;
                case DeliveryMethod.Sequenced:
                    return LiteNetLibChannels.Sequenced;
                case DeliveryMethod.ReliableSequenced:
                    return LiteNetLibChannels.ReliableSequenced;
                default:
                    throw new ArgumentException("Unexpected channel: " + channel);
            }

        }
    }
}
