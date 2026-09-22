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
    private Rigidbody? playerRigidbody = null;
    private bool isAudioPlaying = false;

    private int currentTankCount = 0;

    public int CurrentTankCount => currentTankCount;

    [Header("Used Tank Ejection")]
    public GameObject? usedTankPrefab;
    // how far behind the player (opposite their view direction) the spent tank spawns
    public float usedTankSpawnOffset = 1f;
    // fraction of the player's current velocity the tank inherits, so it drifts along with them
    public float usedTankVelocityPercent = 0.3f;
    public float usedTankMinSpinSpeed = 0.05f;
    public float usedTankMaxSpinSpeed = 0.15f;

    void Awake()
    {
        modifiers = GetComponent<Modifiers>();
        playerController = GetComponent<FirstPersonController>();
        entity = GetComponent<Entity>();
        playerRigidbody = GetComponent<Rigidbody>();
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
            AkUnitySoundEngine.PostEvent("play_tank_used", gameObject);
            SpawnUsedTank();
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

    private void SpawnUsedTank()
    {
        if (usedTankPrefab == null || playerController == null)
        {
            return;
        }

        Camera? cam = playerController.playerCamera;
        Vector3 viewDirection = cam != null ? cam.transform.forward : transform.forward;

        Vector3 spawnPosition = transform.position - viewDirection * usedTankSpawnOffset;
        GameObject tank = Instantiate(usedTankPrefab, spawnPosition, UnityEngine.Random.rotation);

        Rigidbody? tankRigidbody = tank.GetComponent<Rigidbody>();
        if (tankRigidbody != null && playerRigidbody != null)
        {
            tankRigidbody.linearVelocity = playerRigidbody.linearVelocity * usedTankVelocityPercent;
        }

        SlowlyRotate spin = tank.AddComponent<SlowlyRotate>();
        spin.randomizeAxis = true;
        spin.rotationsPerSecond = UnityEngine.Random.Range(usedTankMinSpinSpeed, usedTankMaxSpinSpeed);
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
