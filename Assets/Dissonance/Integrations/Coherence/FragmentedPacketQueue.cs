using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dissonance.Integrations.Coherence
{
    internal class FragmentedPacketQueue
    {
        private uint _currentDataId;
        private List<byte[]> _pendingData;
        private readonly Queue<ArraySegment<byte>> _queuedData = new();

        public bool TryGetData(out ArraySegment<byte> data)
        {
            return _queuedData.TryDequeue(out data);
        }

        /// <remarks>
        /// Assumes the data is reliable and ordered.
        /// </remarks>
        public void AddData(uint dataID, byte[] data, byte index, byte total)
        {
            if (_pendingData == null)
            {
                _pendingData = new List<byte[]>();
                _currentDataId = dataID;
            }

            if (_currentDataId != dataID)
            {
                Debug.LogWarning($"dataID {_currentDataId} != {dataID}");

                // some data was dropped so now we're on new data.
                _pendingData = new List<byte[]>();
                _currentDataId = dataID;

                return;
            }

            if (index != _pendingData.Count)
            {
                Debug.LogWarning($"index {_pendingData.Count} != {index}");

                // some data was dropped so now we're on new data.
                _pendingData = new List<byte[]>();
                _currentDataId = dataID;

                return;
            }

            _pendingData.Add(data);

            if (_pendingData.Count == total)
            {
                var totalData = new List<byte>();
                foreach (var part in _pendingData)
                {
                    totalData.AddRange(part);
                }

                _queuedData.Enqueue(new ArraySegment<byte>(totalData.ToArray()));
                _pendingData = null;
            }
        }
    }
}