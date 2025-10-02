using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Coherence;
using Coherence.Cloud;
using Coherence.Connection;
using Coherence.Toolkit;
using UnityEngine;

namespace Dissonance.Integrations.Coherence.Demo
{
    /// <summary>
    /// Connects to a local Replication Server serving Rooms.
    /// </summary>
    /// <remarks>
    /// It joins the first room tagged as "dissonance_demo",
    /// or creates it if it doesn't exist yet.
    /// </remarks>
    internal class CoherenceJoinNetworkSample : MonoBehaviour
    {
        private CoherenceBridge _bridge;
        private ReplicationServerRoomsService _service;
        private static readonly string[] RoomTags = { "dissonance_demo" };

        private void Awake()
        {
            _service = new ReplicationServerRoomsService();
        }

        private void OnEnable()
        {
            if (!CoherenceBridgeStore.TryGetBridge(gameObject.scene, out _bridge))
            {
                Debug.LogError("Must have a CoherenceBridge on the scene.");
                enabled = false;
                return;
            }

            _bridge.onConnected.AddListener(OnConnected);
            _bridge.onConnectionError.AddListener(OnConnectionError);
            _bridge.onDisconnected.AddListener(OnDisconnected);
        }

        private void OnDisable()
        {
            _bridge.onConnected.RemoveListener(OnConnected);
            _bridge.onConnectionError.RemoveListener(OnConnectionError);
            _bridge.onDisconnected.RemoveListener(OnDisconnected);
        }

        private async void Start()
        {
            try
            {
                var isOnline = await _service.IsOnline();
                if (!isOnline)
                {
                    var rt = RuntimeSettings.Instance;
                    Debug.LogError($"Must start a Replication Server serving Rooms at {rt.LocalHost}:{rt.LocalRoomsUDPPort}");
                    enabled = false;
                    return;
                }

#if HAS_MPPM
                // When using MPPM, let's have a delay on additional editors so that the main editor
                // can create the room, if it doesn't exist yet.
                if (!Unity.Multiplayer.Playmode.CurrentPlayer.IsMainEditor)
                {
                    await Awaitable.WaitForSecondsAsync(3f, destroyCancellationToken);
                }
#endif

                RoomData room;
                var rooms = await _service.FetchRoomsAsync(RoomTags);
                if (rooms.Count == 0)
                {
                    room = await _service.CreateRoomAsync(new RoomCreationOptions
                    {
                        Tags = RoomTags,
                        KeyValues = new Dictionary<string, string>
                        {
                            { RoomData.RoomNameKey, "Dissonance Demo" }
                        }
                    });
                }
                else
                {
                    room = rooms[0];
                }

                _bridge.JoinRoom(room);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void OnDestroy()
        {
            _service?.Dispose();
        }

        private static void OnDisconnected(CoherenceBridge bridge, ConnectionCloseReason closeReason)
        {
            Debug.Log($"Disconnected from Replication Server ({closeReason})", bridge);
        }

        private static void OnConnectionError(CoherenceBridge bridge, ConnectionException connectionException)
        {
            Debug.LogError($"Replication Server connection error.\n{connectionException.GetPrettyMessage().message}", bridge);
        }

        private static void OnConnected(CoherenceBridge bridge)
        {
            Debug.Log("Connected to Replication Server", bridge);
        }
    }
}
