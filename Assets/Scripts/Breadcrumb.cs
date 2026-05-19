#nullable enable

using System;
using Unity;
using UnityEngine;

public class Breadcrumb : MonoBehaviour
{
    public float lifetimeSeconds;
    public float initialAlpha;
    public float fadePowExponent;
    private float counterSeconds;

    public MeshRenderer? materialRenderer;
    private Material? materialInstance = null;

    public void Start()
    {
        materialInstance = materialRenderer?.material;
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
}
