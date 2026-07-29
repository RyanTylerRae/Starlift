#nullable enable

using System.Collections.Generic;
using UnityEngine;

public class LaserHazard : MonoBehaviour
{
    [Header("Toggle Behavior")]
    public bool useAutoToggle = true;
    public float onDuration = 2f;
    public float offDuration = 2f;
    public bool startOn = false;

    [Header("State")]
    public bool isLaserOn = false;

    [Header("Visualization")]
    public Color onColor = Color.red;
    public Color offColor = new Color(1f, 0f, 0f, 0.2f);

    private HashSet<Entity> entitiesInTrigger = new HashSet<Entity>();
    private float toggleTimer = 0f;

    private void OnEnable()
    {
        isLaserOn = startOn;
        toggleTimer = 0f;
    }

    private void Update()
    {
        if (!useAutoToggle)
        {
            return;
        }

        toggleTimer += Time.deltaTime;
        float currentDuration = isLaserOn ? onDuration : offDuration;

        if (toggleTimer >= currentDuration)
        {
            toggleTimer = 0f;
            SetLaserOn(!isLaserOn);
        }
    }

    public void Toggle()
    {
        SetLaserOn(!isLaserOn);
    }

    public void SetLaserOn(bool on)
    {
        isLaserOn = on;

        if (isLaserOn)
        {
            foreach (Entity entity in new List<Entity>(entitiesInTrigger))
            {
                if (entity != null && entity.IsAlive)
                {
                    KillEntity(entity);
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Entity entity))
        {
            entitiesInTrigger.Add(entity);

            if (isLaserOn && entity.IsAlive)
            {
                KillEntity(entity);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out Entity entity))
        {
            entitiesInTrigger.Remove(entity);
        }
    }

    private void KillEntity(Entity entity)
    {
        DamageEvent damageEvent = new();
        damageEvent.damageTarget = entity.gameObject;
        damageEvent.damageSource = gameObject;
        damageEvent.damageType = DamageType.Laser;

        entity.Kill(damageEvent);
        entitiesInTrigger.Remove(entity);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = isLaserOn ? onColor : offColor;

        if (TryGetComponent(out BoxCollider boxCollider))
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(boxCollider.center, boxCollider.size);
        }
    }
}
