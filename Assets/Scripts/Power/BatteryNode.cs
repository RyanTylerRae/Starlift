using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BatteryNode : MonoBehaviour, IPowerNode
{
    public float powerCapacity;
    public float currentPower;

    public float maxChargeRate;
    public float maxDischargeRate;

    public List<IPowerNode> OutputNodes = new();

    public float ReportDemand()
    {
        float remaining = powerCapacity - currentPower;
        return Mathf.Min(remaining, maxChargeRate);
    }

    public float ReceivePacket(PowerPacket powerPacket)
    {
        float acceptablePower = Mathf.Min(powerPacket.energy, ReportDemand());
        currentPower += acceptablePower;
        return acceptablePower;
    }

    public void DistributeToOutputs()
    {
        if (OutputNodes.Count == 0 || currentPower == 0.0f)
        {
            return;
        }

        float totalDemand = OutputNodes.Sum(output => output.ReportDemand());
        if (totalDemand == 0.0f)
        {
            return;
        }

        float energyToDistribute = Mathf.Min(currentPower, maxDischargeRate);
        float energyDistributed = 0.0f;

        PowerUtils powerUtils = Object.FindFirstObjectByType<PowerUtils>();

        foreach (var outputNode in OutputNodes.OrderBy(output => (output as PowerConsumer)?.priority ?? EPowerPriority.Unassigned))
        {
            if (energyToDistribute == 0.0f)
            {
                break;
            }

            float demand = outputNode.ReportDemand();
            float sentEnergy = Mathf.Min(demand, energyToDistribute);
            sentEnergy = outputNode.ReceivePacket(new PowerPacket() { energy = sentEnergy });

            powerUtils.QueueNumber(sentEnergy, transform.position + (transform.position - outputNode.GetTransform().position) * 0.5f);

            energyDistributed += sentEnergy;
            energyToDistribute -= sentEnergy;
        }

        currentPower -= Mathf.Max(0.0f, currentPower - energyDistributed);
    }

    public Transform GetTransform()
    {
        return transform;
    }
}