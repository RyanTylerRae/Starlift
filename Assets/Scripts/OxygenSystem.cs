#nullable enable

using System;
using UnityEngine;

public class OxygenSystem : MonoBehaviour
{
    public float usagePerSecond;
    public float thrustMultiplier;
    public float sprintMultiplier;
    public float magnetizedWalkMultiplier;
    private Modifiers? modifiers = null;
    private FirstPersonController? playerController = null;
    private Entity? entity = null;
    private bool isAudioPlaying = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        modifiers = GetComponent<Modifiers>();
        playerController = GetComponent<FirstPersonController>();
        entity = GetComponent<Entity>();
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

    void LateUpdate()
    {
        if (modifiers == null || playerController == null || entity == null || !entity.IsAlive)
        {
            return;
        }

        float oxygenAmount = modifiers.Get(ModifierType.Oxygen);

        if (playerController.OxygenBurnRate > 0.0f)
        {
            oxygenAmount -= Time.deltaTime * usagePerSecond * thrustMultiplier * playerController.OxygenBurnRate;
        }
        else if (playerController.IsSprinting)
        {
            oxygenAmount -= Time.deltaTime * usagePerSecond * sprintMultiplier;
        }
        else if (playerController.IsMagnetizedWalking)
        {
            oxygenAmount -= Time.deltaTime * usagePerSecond * magnetizedWalkMultiplier;
        }
        else
        {
            oxygenAmount -= Time.deltaTime * usagePerSecond;
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
            entity.SendDamageEvent(gameObject, 100, DamageType.Suffocating);

            if (isAudioPlaying)
            {
                Debug.Log("TRAE death event!");
                AkUnitySoundEngine.PostEvent("stop_blend_breathing", gameObject);
                AkUnitySoundEngine.PostEvent("play_breathing_death", gameObject);
                isAudioPlaying = false;
            }
        }
    }

    public void DepleteOxygen(float amount)
    {
        if (modifiers == null || amount <= 0.0f)
        {
            return;
        }

        float oxygenAmount = modifiers.Get(ModifierType.Oxygen);
        oxygenAmount -= amount;
        modifiers.Set(ModifierType.Oxygen, oxygenAmount);
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
