#nullable enable

using UnityEngine;
using System;

public class Health : MonoBehaviour
{
    public int maxHealth;
    private int health;

    private Entity? entity;

    public void Start()
    {
        TryGetComponent(out entity);

        health = maxHealth;

        var damageEventHandler = GetComponentInChildren<DamageEventHandler>();
        if (damageEventHandler == null)
        {
            Debug.LogError("A Health component does not have an accessible damage event handler");
        }
        else
        {
            damageEventHandler.OnDamaged += OnDamageEvent;
        }
    }

    public void OnDamageEvent(DamageEvent damageEvent)
    {
        health -= damageEvent.damageAmount;
        if (health <= 0)
        {
            health = 0;
            entity?.Kill(damageEvent);
        }
    }
}
