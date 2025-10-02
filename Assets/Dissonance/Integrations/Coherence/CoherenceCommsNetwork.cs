using System;
using System.Collections.Generic;
using Coherence;
using Coherence.Connection;
using Coherence.Toolkit;
using Dissonance.Networking;
using UnityEngine;

namespace Dissonance.Integrations.Coherence
{
    [RequireComponent(typeof(DissonanceComms))]
    [RequireComponent(typeof(CoherenceSync))]
    [AddComponentMenu("coherence/Voice/Dissonance/Coherence Comms Network")]
    [DefaultExecutionOrder(ScriptExecutionOrder.CoherenceSync + 10)]
    public class CoherenceCommsNetwork : BaseCommsNetwork<
        CoherenceServer,
        CoherenceClient,
        ClientID,
        Unit,
        Unit>
    {
        [SerializeField] private float staleAudioTimeSeconds = 3;

        private CoherenceSync _sync;
        private CoherenceBridge _bridge;
        private CoherenceClientConnection _myClientConnection;
        private CoherenceClient _myClient;
        private bool _liveQuerySynced;
        private bool _commsStarted;
        private readonly FragmentedPacketSender<CoherenceCommsNetwork> _packetSender = new();
        private readonly Dictionary<ClientID, FragmentedPacketQueue> _receivedData = new();

        public CoherenceBridge Bridge => _bridge;

        public CoherenceSyncVoice GetCommsClient(ClientID clientID)
        {
            var connection = _bridge.ClientConnections.Get(clientID);
            return connection?.GameObject.GetComponent<CoherenceSyncVoice>();
        }

        public void SendReliableServerDissonanceData(ArraySegment<byte> data)
        {
            _packetSender.SendDissonanceData(_sync, nameof(ReceiveReliableDissonanceData), data, true);
        }

        public void SendUnreliableServerDissonanceData(ArraySegment<byte> data)
        {
            _packetSender.SendDissonanceData(_sync, nameof(ReceiveUnreliableDissonanceData), data, false);
        }

        [Command]
        public void ReceiveReliableDissonanceData(byte[] data,
            uint dataID,
            byte index,
            byte total,
            uint senderId,
            long sendTime)
        {
            var clientId = (ClientID)senderId;
            if (!_receivedData.TryGetValue(clientId, out var fragmentQueue))
            {
                fragmentQueue = new FragmentedPacketQueue();
                _receivedData.Add(clientId, fragmentQueue);
            }

            fragmentQueue.AddData(dataID, data, index, total);
        }

        [Command]
        public void ReceiveUnreliableDissonanceData(byte[] data,
            uint dataID,
            byte index,
            byte total,
            uint senderId,
            long sendTime)
        {
            var timeDiff = DateTime.UtcNow - new DateTime(sendTime);
            if (timeDiff > TimeSpan.FromSeconds(staleAudioTimeSeconds))
            {
                return;
            }

            var clientId = (ClientID)senderId;
            if (!_receivedData.TryGetValue(clientId, out var fragmentQueue))
            {
                fragmentQueue = new FragmentedPacketQueue();
                _receivedData.Add(clientId, fragmentQueue);
            }

            fragmentQueue.AddData(dataID, data, index, total);
        }

        public void TryGetServerData(Action<ClientID, ArraySegment<byte>> onReceivedData)
        {
            foreach (var (clientID, queue) in _receivedData)
            {
                while (queue.TryGetData(out var data))
                {
                    onReceivedData(clientID, data);
                }
            }
        }

        [Command]
        public void RestartComms()
        {
            _commsStarted = false;
            Stop();

            if (!_liveQuerySynced || !_sync || !_bridge)
            {
                Log.Warn(nameof(RestartComms) + " called but the network is not ready.");
                return;
            }

            if (_sync.HasStateAuthority)
            {
                RunAsHost(Unit.None, Unit.None);
            }
            else
            {
                RunAsClient(Unit.None);
            }

            _commsStarted = true;
        }

        protected virtual void Awake()
        {
            var hasSync = TryGetComponent(out _sync);
            Log.AssertAndLogError(hasSync, "1487A024-E565-46B2-84D6-9AC2EEF275F8", $"Required {nameof(CoherenceSync)} component not found on {nameof(CoherenceCommsNetwork)}.", this);
        }

        protected virtual void OnEnable()
        {
            _bridge = _sync.CoherenceBridge;
            if (!_bridge)
            {
                Log.Error("CoherenceBridge required on the scene to network voice.", this);
                enabled = false;
                return;
            }

            if (!_bridge.EnableClientConnections)
            {
                Log.Error("Client Connections are required to network voice.", _bridge);
                enabled = false;
                return;
            }

            _sync.OnStateAuthority.AddListener(OnStateAuthority);
            _bridge.onLiveQuerySynced.AddListener(OnLiveQuerySynced);
            _bridge.onDisconnected.AddListener(OnBridgeDisconnected);
        }

        protected override void OnDisable()
        {
            if (_sync != null)
            {
                _sync.OnStateAuthority.RemoveListener(OnStateAuthority);
            }

            if (_bridge != null)
            {
                _bridge.onLiveQuerySynced.RemoveListener(OnLiveQuerySynced);
                _bridge.onDisconnected.RemoveListener(OnBridgeDisconnected);
            }

            base.OnDisable();
        }

        protected override void Update()
        {
            if (!IsInitialized)
            {
                return;
            }

            if (!_commsStarted && _liveQuerySynced)
            {
                RestartComms();
            }

            base.Update();
        }

        protected override CoherenceServer CreateServer(Unit connectionParameters)
        {
            return new CoherenceServer(this);
        }

        protected override CoherenceClient CreateClient(Unit connectionParameters)
        {
            var clientConnection = _bridge.ClientConnections.GetMine();
            return new CoherenceClient(this, clientConnection);
        }

        private void OnLiveQuerySynced(CoherenceBridge bridge)
        {
            _liveQuerySynced = true;
        }

        private void OnStateAuthority()
        {
            if (!_commsStarted)
            {
                return;
            }

            _sync.SendCommand<CoherenceCommsNetwork>(nameof(RestartComms), MessageTarget.All);
        }

        private void OnBridgeDisconnected(CoherenceBridge _, ConnectionCloseReason __)
        {
            _liveQuerySynced = false;
            _commsStarted = false;
            Stop();
        }
    }
}
