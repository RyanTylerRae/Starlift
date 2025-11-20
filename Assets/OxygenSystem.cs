#nullable enable

using UnityEngine;

public class OxygenSystem : MonoBehaviour
{
    public float usagePerSecond;
    private Modifiers? modifiers = null;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        modifiers = GetComponent<Modifiers>();
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
