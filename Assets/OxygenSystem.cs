#nullable enable

using System;
using UnityEngine;

public class OxygenSystem : MonoBehaviour
{
    public float usagePerSecond;
    private Modifiers? modifiers = null;
    private bool isAudioPlaying = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        modifiers = GetComponent<Modifiers>();

        AkUnitySoundEngine.PostEvent("play_blend_breathing", gameObject);
        isAudioPlaying = true;
    }

    void OnDestroy()
    {
        AkUnitySoundEngine.PostEvent("stop_blend_breathing", gameObject);
        isAudioPlaying = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (modifiers == null)
        {
            return;
        }

        float oxygenAmount = modifiers.Get(ModifierType.Oxygen);
        oxygenAmount -= Time.deltaTime * usagePerSecond;

        if (oxygenAmount > 0.0f)
        {
            if (!isAudioPlaying)
            {
                AkUnitySoundEngine.PostEvent("play_blend_breathing", gameObject);
                isAudioPlaying = true;
            }

            modifiers.Set(ModifierType.Oxygen, oxygenAmount);
            AkUnitySoundEngine.SetRTPCValue("PlayerOxygenAmount", oxygenAmount);
        }
        else
        {
            oxygenAmount = 0.0f;

            if (TryGetComponent(out Entity entity))
            {
                entity.SendDamageEvent(gameObject, 999, DamageType.None);
            }

            if (isAudioPlaying)
            {
                AkUnitySoundEngine.PostEvent("stop_blend_breathing", gameObject);
                AkUnitySoundEngine.PostEvent("play_breathing_death", gameObject);
                isAudioPlaying = false;
            }
        }
    }

    public void ReplenishOxygen(float replenishRate)
    {
        if (modifiers == null)
        {
            return;
        }

        float oxygenAmount = modifiers.Get(ModifierType.Oxygen);
        oxygenAmount += Time.deltaTime * replenishRate;
        modifiers.Set(ModifierType.Oxygen, oxygenAmount);
    }
}
