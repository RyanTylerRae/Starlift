using System.Collections.Generic;
using UnityEngine;

public class EntityDamager : MonoBehaviour
{
    [SerializeField] private int damageAmount = 10;
    [SerializeField] private float damageInterval = 1f;
    [SerializeField] private DamageType damageType = DamageType.None;

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
        Entity entity = other.GetComponent<Entity>();
        if (entity != null)
        {
            entitiesInTrigger.Add(entity);
        }
    }

    void OnTriggerExit(Collider other)
    {
        Entity entity = other.GetComponent<Entity>();
        if (entity != null)
        {
            entitiesInTrigger.Remove(entity);
        }
    }
}
