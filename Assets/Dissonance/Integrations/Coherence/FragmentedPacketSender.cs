using System;
using Coherence;
using Coherence.Serializer;
using Coherence.Toolkit;
using UnityEngine;

namespace Dissonance.Integrations.Coherence
{
    internal class FragmentedPacketSender<T> where T : Component
    {
        private const int MaxSegmentSize = OutProtocolBitStream.BYTES_LIST_MAX_LENGTH;
        private uint _dataId;

        public void SendDissonanceData(CoherenceSync sync, string methodName, ArraySegment<byte> data, bool reliable)
        {
            var cursor = 0;
            var index = 0;
            var total = (int)Mathf.Ceil((float)data.Count / MaxSegmentSize);
            var sendTime = DateTime.UtcNow.Ticks;

            while (cursor < data.Count)
            {
                var nextPos = Mathf.Clamp(cursor + MaxSegmentSize, cursor, data.Count);
                var dist = nextPos - cursor;

                Func<string, MessageTarget, object[], bool> sendCommand = reliable
                    ? sync.SendOrderedCommand<T>
                    : sync.SendCommand<T>;

                sendCommand(methodName,
                    MessageTarget.AuthorityOnly,
                    new object[]
                    {
                        data.Slice(cursor, dist).ToArray(),
                        _dataId,
                        (byte)index,
                        (byte)total,
                        (uint)sync.CoherenceBridge.ClientID,
                        sendTime,
                    });

                index++;
                cursor = nextPos;
            }

            _dataId = (_dataId + 1) % uint.MaxValue;
        }
    }
}