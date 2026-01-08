#nullable enable

using System;
using UnityEngine;

public class OxygenSystem : MonoBehaviour
{
    public float usagePerSecond;
    public float thrustMultiplier;
    private Modifiers? modifiers = null;
    private FirstPersonController? playerController = null;
    private bool isAudioPlaying = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        modifiers = GetComponent<Modifiers>();
        playerController = GetComponent<FirstPersonController>();
        isAudioPlaying = true;
    }

    void OnDestroy()
    {
        isAudioPlaying = false;
    }

    void OnEnable()
    {
        AkUnitySoundEngine.PostEvent("play_blend_breathing", gameObject);
    }

    void OnDisable()
    {
        AkUnitySoundEngine.PostEvent("stop_blend_breathing", gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        if (modifiers == null || playerController == null)
        {
            return;
        }

        float oxygenAmount = modifiers.Get(ModifierType.Oxygen);

        if (!playerController.IsBurningOxygen)
        {
            oxygenAmount -= Time.deltaTime * usagePerSecond;
        }
        else
        {
            oxygenAmount -= Time.deltaTime * usagePerSecond * thrustMultiplier;
        }

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
                entity.SendDamageEvent(gameObject, 100, DamageType.Suffocating);
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
