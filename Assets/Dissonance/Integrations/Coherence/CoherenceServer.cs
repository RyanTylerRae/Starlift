using System;
using Coherence.Connection;
using Coherence.Toolkit;
using Dissonance.Networking;

namespace Dissonance.Integrations.Coherence
{
    public class CoherenceServer : BaseServer<CoherenceServer, CoherenceClient, ClientID>
    {
        private readonly CoherenceCommsNetwork _network;

        public CoherenceServer(CoherenceCommsNetwork network)
        {
            Log.AssertAndLogError(network, "44548B74-D1C6-4BD9-BEB7-B4CBA36E46D4", $"{nameof(CoherenceCommsNetwork)} null at server creation time.");
            _network = network;
        }

        public override void Connect()
        {
            Log.AssertAndLogError(_network, "E2D1D54C-37E6-4F6C-8DE9-5AC17E89D0DE", $"{nameof(CoherenceCommsNetwork)} null at server connection time.");
            _network.Bridge.ClientConnections.OnDestroyed += OnClientConnectionDestroyed;
        }

        public override void Disconnect()
        {
            if (_network && _network.Bridge && _network.Bridge.ClientConnections != null)
            {
                _network.Bridge.ClientConnections.OnDestroyed -= OnClientConnectionDestroyed;
            }

            base.Disconnect();
        }

        protected override void ReadMessages()
        {
            Log.AssertAndLogError(_network, "A1F3884D-3CD1-4C3D-B8C2-D7E473A2BD9A", $"{nameof(CoherenceCommsNetwork)} null at server message reading time.");
            _network.TryGetServerData(NetworkReceivedPacket);
        }

        protected override void SendReliable(ClientID clientId, ArraySegment<byte> packet)
        {
            Log.AssertAndLogError(_network, "0C25909E-1CA5-4170-A21C-EB46C41B30C2", $"{nameof(CoherenceCommsNetwork)} null at server send reliable time.");
            var commsClient = _network.GetCommsClient(clientId);
            commsClient?.SendReliableClientDissonanceData(packet);
        }

        protected override void SendUnreliable(ClientID clientId, ArraySegment<byte> packet)
        {
            Log.AssertAndLogError(_network, "27418FAB-381C-4FF5-B6EA-3C892D838E9E", $"{nameof(CoherenceCommsNetwork)} null at server send reliable time.");
            var commsClient = _network.GetCommsClient(clientId);
            commsClient?.SendUnreliableClientDissonanceData(packet);
        }

        private void OnClientConnectionDestroyed(CoherenceClientConnection clientConnection)
        {
            ClientDisconnected(clientConnection.ClientId);
        }
    }
}
