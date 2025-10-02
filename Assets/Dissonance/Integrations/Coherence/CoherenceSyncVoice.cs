using System;
using System.Collections.Generic;
using Coherence;
using Coherence.Connection;
using Coherence.Toolkit;
using UnityEngine;

namespace Dissonance.Integrations.Coherence
{
    [RequireComponent(typeof(CoherenceSync))]
    [AddComponentMenu("coherence/Voice/Dissonance/Coherence Sync Voice")]
    public class CoherenceSyncVoice : MonoBehaviour
    {
        [SerializeField] private float _staleAudioTimeSeconds = 3;

        private CoherenceSync _sync;
        private readonly FragmentedPacketSender<CoherenceSyncVoice> _packetSender = new();
        private readonly Dictionary<ClientID, FragmentedPacketQueue> _receivedData = new();

        [Sync]
        public uint ClientID { get; set; }

        private void Awake()
        {
            var hasSync = TryGetComponent(out _sync);
            Debug.Assert(hasSync, $"Required {nameof(CoherenceSync)} component not found on {nameof(CoherenceSyncVoice)}.", this);
        }

        private void Start()
        {
            if (_sync.HasStateAuthority)
            {
                ClientID = (uint)_sync.CoherenceBridge.ClientID;
            }
        }

        /// <remarks>
        /// Called by <see cref="CoherenceServer"/>.
        /// Sends the voice data through <see cref="CoherenceSync.SendOrderedCommand{TTarget}(string, MessageTarget, object[])"/>.
        /// Data is received by the <see cref="CoherenceSyncVoice"/> that holds state authority.
        /// </remarks>
        public void SendReliableClientDissonanceData(ArraySegment<byte> data)
        {
            _packetSender.SendDissonanceData(_sync, nameof(ReceiveReliableDissonanceData), data, true);
        }

        /// <remarks>
        /// Called by <see cref="CoherenceServer"/>.
        /// Sends the voice data to the state authority through <see cref="CoherenceSync.SendCommand{TTarget}(string, MessageTarget, object[])"/>.
        /// Data is received in <see cref="ReceiveReliableDissonanceData"/>.
        /// </remarks>
        public void SendUnreliableClientDissonanceData(ArraySegment<byte> data)
        {
            _packetSender.SendDissonanceData(_sync, nameof(ReceiveUnreliableDissonanceData), data, false);
        }

        /// <remarks>
        /// Called by <see cref="SendReliableClientDissonanceData"/>.
        /// </remarks>
        [Command]
        public void ReceiveReliableDissonanceData(byte[] data,
            uint dataId,
            byte index,
            byte total,
            uint senderId,
            long sendTime)
        {
            var id = (ClientID)senderId;
            if (!_receivedData.TryGetValue(id, out var fragmentQueue))
            {
                fragmentQueue = new FragmentedPacketQueue();
                _receivedData.Add(id, fragmentQueue);
            }

            fragmentQueue.AddData(dataId, data, index, total);
        }

        /// <remarks>
        /// Called by <see cref="SendUnreliableClientDissonanceData"/>.
        /// </remarks>
        [Command]
        public void ReceiveUnreliableDissonanceData(byte[] data,
            uint dataId,
            byte index,
            byte total,
            uint senderId,
            long sendTime)
        {
            var timeDiff = DateTime.UtcNow - new DateTime(sendTime);
            if (timeDiff > TimeSpan.FromSeconds(_staleAudioTimeSeconds))
            {
                return;
            }

            var id = (ClientID)senderId;
            if (!_receivedData.TryGetValue(id, out var fragmentQueue))
            {
                fragmentQueue = new FragmentedPacketQueue();
                _receivedData.Add(id, fragmentQueue);
            }

            fragmentQueue.AddData(dataId, data, index, total);
        }

        /// <remarks>
        /// Called by <see cref="CoherenceClient"/>, to pull any received voice data.
        /// </remarks>
        public void TryGetClientData(Func<ArraySegment<byte>, ushort?> onReceivedData)
        {
            foreach (var receivedData in _receivedData)
            {
                while (receivedData.Value.TryGetData(out var data))
                {
                    _ = onReceivedData(data);
                }
            }
        }
    }
}
