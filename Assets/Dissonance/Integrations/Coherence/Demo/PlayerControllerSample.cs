using UnityEngine;
using Coherence.Toolkit;

namespace Dissonance.Integrations.Coherence.Demo
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Transform))]
    [RequireComponent(typeof(CoherenceSync))]
    public class PlayerControllerSample : MonoBehaviour
    {
        private CharacterController _controller;
        private Transform _transform;
        private CoherenceSync _sync;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _transform = GetComponent<Transform>();
            _sync = GetComponent<CoherenceSync>();
        }

        private void Update()
        {
            if (_sync && !_sync.HasStateAuthority)
            {
                return;
            }

            var rotation = Input.GetAxis("Horizontal") * Time.deltaTime * 150.0f;
            var speed = Input.GetAxis("Vertical") * 3.0f;

            _transform.Rotate(0, rotation, 0);
            _controller.SimpleMove(_transform.TransformDirection(Vector3.forward) * speed);

            // Reset position if the player falls below a certain height
            if (_transform.position.y < -3)
            {
                _transform.position = Vector3.zero;
                _transform.rotation = Quaternion.identity;
            }
        }
    }
}
