using System;
using UnityEngine;

public class DamageEventHandler : MonoBehaviour
{
    public event Action<DamageEvent> OnDamaged = delegate { };

    public void HandleDamageEvent(DamageEvent damageEvent)
    {
        OnDamaged?.Invoke(damageEvent);
    }
}
