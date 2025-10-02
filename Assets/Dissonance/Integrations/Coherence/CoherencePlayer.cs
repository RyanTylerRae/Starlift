using System;
using Coherence.Toolkit;
using UnityEngine;
using Logger = Coherence.Log.Logger;
using CoherenceLog = Coherence.Log.Log;

namespace Dissonance.Integrations.Coherence
{
    [RequireComponent(typeof(CoherenceSync))]
    [AddComponentMenu("coherence/Voice/Dissonance/Coherence Player")]
    [DefaultExecutionOrder(ScriptExecutionOrder.CoherenceSync + 20)]
    public class CoherencePlayer : MonoBehaviour, IDissonancePlayer
    {
        public string PlayerId => playerId;
        public Vector3 Position => _listener ? _listener.position : transform.position;
        public Quaternion Rotation => _listener ? _listener.rotation : transform.rotation;
        public NetworkPlayerType Type => _sync.HasInputAuthority ? NetworkPlayerType.Local : NetworkPlayerType.Remote;
        public bool IsTracking => isTracking;

        [SerializeField] private bool _autoStartTracking = true;
        [Tooltip("Sets the Transform used for proximity tracking. If not set, the Transform on this GameObject will be used.")]
        [SerializeField] private Transform _listener;

        private CoherenceSync _sync;
        private Logger _logger;
        private DissonanceComms _dissonanceComms;

        [Sync, NonSerialized]
        public string playerId;
        [Sync, NonSerialized, OnValueSynced(nameof(OnTrackingChanged))]
        public bool isTracking;

        public void StartTracking()
        {
            playerId = _dissonanceComms.LocalPlayerName;
            _logger.Debug($"Start tracking {_sync.name} ({_sync.EntityState.EntityID})",
                ("auth", _sync.HasStateAuthority));
            _dissonanceComms.TrackPlayerPosition(this);
            isTracking = true;
        }

        public void StopTracking()
        {
            isTracking = false;
            _logger.Debug($"Stop tracking {_sync.name} ({_sync.EntityState.EntityID})",
                ("auth", _sync.HasStateAuthority));
            _dissonanceComms.StopTracking(this);
        }

        public void OnTrackingChanged(bool _, bool startTracking)
        {
            if (startTracking)
            {
                _dissonanceComms.TrackPlayerPosition(this);
            }
            else
            {
                _dissonanceComms.StopTracking(this);
            }
        }

        private void Awake()
        {
            _logger = CoherenceLog.GetLogger<CoherencePlayer>(this);
            var hasSync = TryGetComponent(out _sync);
            Debug.Assert(hasSync, $"Required {nameof(CoherenceSync)} component not found on {nameof(CoherencePlayer)}.", this);
        }

        private void OnEnable()
        {
            if (!_sync.CoherenceBridge)
            {
                _logger.Info($"{nameof(CoherenceBridge)} not found. Disabling component.");
                enabled = false;
                return;
            }

            _dissonanceComms = DissonanceComms.GetSingleton();
            if (!_dissonanceComms)
            {
                _logger.Info($"{nameof(DissonanceComms)} not found. Disabling component.");
                enabled = false;
                return;
            }

            var isMySync = !_sync.CoherenceBridge.IsConnected || _sync.HasStateAuthority;
            if (isMySync && _autoStartTracking)
            {
                var playerName = _dissonanceComms.LocalPlayerName;
                if (string.IsNullOrEmpty(playerName))
                {
                    _dissonanceComms.LocalPlayerNameChanged += OnLocalPlayerNameChanged;
                    return;
                }

                StartTracking();
            }
            else if (!isMySync && isTracking)
            {
                StartTracking();
            }
        }

        private void OnDisable()
        {
            if (_dissonanceComms)
            {
                _dissonanceComms.LocalPlayerNameChanged -= OnLocalPlayerNameChanged;
            }

            StopTracking();
        }

        private void OnLocalPlayerNameChanged(string playerName) => StartTracking();
    }
}
