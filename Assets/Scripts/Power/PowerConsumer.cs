using UnityEngine;

public enum EPowerPriority
{
    Essential,
    Unassigned
}

public class PowerConsumer : MonoBehaviour
{
    public float requiredEnergy;
    public EPowerPriority priority;
    public bool hasPower;

    // Made public so PowerProducer can check current energy level
    public float energyReceivedThisFrame;

    // Called by PowerProducer to deliver energy
    public void ReceiveEnergy(float amount)
    {
        energyReceivedThisFrame += amount;
    }

    public void UseEnergy()
    {
        hasPower = energyReceivedThisFrame >= requiredEnergy;
        energyReceivedThisFrame = 0.0f;
    }

    private void Start()
    {
        PowerController.Instance.AddPowerConsumer(this);
    }

    private void OnDestroy()
    {
        PowerController.Instance.RemovePowerConsumer(this);
    }
}
