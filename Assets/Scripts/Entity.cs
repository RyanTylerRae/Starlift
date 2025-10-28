#nullable enable

using UnityEngine;
using System;

public class Entity : MonoBehaviour
{
    private DamageEventHandler? damageEventHandler = null;
    public event Action<DamageEvent> OnKilled = delegate { };

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        TryGetComponent(out damageEventHandler);
    }

    public void SendDamageEvent(GameObject source, int damage, DamageType damageType)
    {
        if (damageEventHandler != null)
        {
            DamageEvent damageEvent = new();
            damageEvent.damageTarget = gameObject;
            damageEvent.damageSource = source;
            damageEvent.damageAmount = damage;
            damageEvent.damageType = damageType;

            damageEventHandler.HandleDamageEvent(damageEvent);
        }
    }

    public void Kill(DamageEvent damageEvent)
    {
        OnKilled.Invoke(damageEvent);
        Destroy(gameObject);
    }
}
