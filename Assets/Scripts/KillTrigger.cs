using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class KillTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent(out FirstPersonController controller))
        {
            if (other.gameObject.TryGetComponent(out Entity entity))
            {
                DamageEvent damageEvent = new();
                entity?.Kill(damageEvent);
            }
        }
    }
}
