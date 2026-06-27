#nullable enable

using System.Collections.Generic;
using UnityEngine;

public class EntityDamager : MonoBehaviour
{
    public int damageAmount = 10;
    public float damageInterval = 1f;
    public DamageType damageType = DamageType.None;

    private HashSet<Entity> entitiesInTrigger = new HashSet<Entity>();
    private float damageTimer = 0f;

    void Update()
    {
        damageTimer += Time.deltaTime;

        if (damageTimer >= damageInterval)
        {
            damageTimer = 0f;

            entitiesInTrigger.RemoveWhere(entity => entity == null);

            foreach (Entity entity in entitiesInTrigger)
            {
                entity.SendDamageEvent(gameObject, damageAmount, damageType);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Entity entity))
        {
            entitiesInTrigger.Add(entity);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out Entity entity))
        {
            entitiesInTrigger.Remove(entity);
        }
    }
}
