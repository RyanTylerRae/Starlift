#nullable enable

using UnityEngine;

public enum DamageType
{
    None,
    Suffocating
}

public struct DamageEvent
{
    public GameObject? damageTarget;
    public GameObject? damageSource;
    public int damageAmount;
    public DamageType damageType;
}
