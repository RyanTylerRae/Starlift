
using UnityEngine;

public enum EPowerPriority
{
    Essential,
    Unassigned
}

public class PowerConsumer : MonoBehaviour, IPowerNode
{
    public float requiredPower;
    public EPowerPriority priority;
    public bool hasPower;
    public float energyReceivedThisFrame;

    public float ReportDemand()
    {
        return requiredPower;
    }

    public float ReceivePacket(PowerPacket powerPacket)
    {
        energyReceivedThisFrame += powerPacket.energy;
        return powerPacket.energy;
    }

    public void UpdatePower()
    {
        hasPower = energyReceivedThisFrame >= requiredPower;
        energyReceivedThisFrame = 0.0f;
    }

    public Transform GetTransform()
    {
        return transform;
    }
}
