#nullable enable

using System;
using Unity;
using UnityEngine;

public class Breadcrumb : MonoBehaviour
{
    public float lifetimeSeconds;
    public float initialAlpha;
    public float fadePowExponent;
    private float counterSeconds = 0f;

    public MeshRenderer? materialRenderer;
    private Material? materialInstance = null;

    public void Start()
    {
        materialInstance = materialRenderer?.material;

        // destroy self when the player dies
        var entity = StarliftStatics.FindPlayer()?.GetComponentInChildren<Entity>();
        if (entity != null)
        {
            entity.OnKilled += OnPlayerKilled;
        }
    }

    public void OnDestroy()
    {
        var entity = StarliftStatics.FindPlayer()?.GetComponentInChildren<Entity>();
        if (entity != null)
        {
            entity.OnKilled -= OnPlayerKilled;
        }
    }

    public void Update()
    {
        counterSeconds += Time.deltaTime;
        if (counterSeconds > lifetimeSeconds)
        {
            Destroy(gameObject);
        }

        float ratio = counterSeconds / lifetimeSeconds;
        float alpha = initialAlpha * (1.0f - (float)Math.Pow(ratio, fadePowExponent));

        materialInstance?.SetFloat("_Alpha", alpha);
    }

    public void OnPlayerKilled(DamageEvent _)
    {
        Destroy(gameObject);
    }
}
