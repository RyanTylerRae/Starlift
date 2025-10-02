using System;
using Coherence.Connection;
using Coherence.Toolkit;
using Dissonance.Networking;

namespace Dissonance.Integrations.Coherence
{
    public class CoherenceClient : BaseClient<CoherenceServer, CoherenceClient, ClientID>
    {
        private readonly CoherenceCommsNetwork _network;
        private readonly CoherenceSyncVoice _mySyncVoice;

        public CoherenceClient(CoherenceCommsNetwork network, CoherenceClientConnection clientConnection) : base(network)
        {
            _network = network;

            var go = clientConnection.GameObject;
            var hasSyncVoice = go.TryGetComponent(out _mySyncVoice);
            Log.AssertAndLogError(hasSyncVoice, "C20E55D6-9807-4FDE-A3BE-01078A4663A7", $"Client Connection has no {nameof(CoherenceSyncVoice)} component attached to it. Connection not established.", go);
        }

        public override void Connect()
        {
            Connected();
        }

        protected override void ReadMessages()
        {
            _mySyncVoice.TryGetClientData(NetworkReceivedPacket);
        }

        protected override void SendReliable(ArraySegment<byte> packet)
        {
            _network.SendReliableServerDissonanceData(packet);
        }

        protected override void SendUnreliable(ArraySegment<byte> packet)
        {
            _network.SendUnreliableServerDissonanceData(packet);
        }
    }
}
