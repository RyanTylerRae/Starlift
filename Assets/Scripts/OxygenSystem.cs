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

    private int currentTankCount = 0;

    public int CurrentTankCount => currentTankCount;

    void Awake()
    {
        modifiers = GetComponent<Modifiers>();
        playerController = GetComponent<FirstPersonController>();
        entity = GetComponent<Entity>();
        isAudioPlaying = true;

        currentTankCount = GameState.Instance.saveData.maxOxygenTankCount;
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
        if (modifiers == null || playerController == null || entity == null || !entity.IsAlive || PlayerSettings.GodModeOxygenDisabled)
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
        }
        else if (currentTankCount > 1)
        {
            --currentTankCount;
            oxygenAmount = modifiers.GetMax(ModifierType.Oxygen);

            AkUnitySoundEngine.PostEvent("play_oxygen_replenish", gameObject);
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

        modifiers.Set(ModifierType.Oxygen, oxygenAmount);
        AkUnitySoundEngine.SetRTPCValue("PlayerOxygenAmount", oxygenAmount);
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

        if (oxygenAmount > modifiers.GetMax(ModifierType.Oxygen) && currentTankCount < GameState.Instance.saveData.maxOxygenTankCount)
        {
            oxygenAmount -= modifiers.GetMax(ModifierType.Oxygen);
            ++currentTankCount;
        }

        modifiers.Set(ModifierType.Oxygen, oxygenAmount);
    }
}
