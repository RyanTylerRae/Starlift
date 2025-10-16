using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PowerJunction : MonoBehaviour, IPowerNode
{
    public List<IPowerNode> OutputNodes = new();

    public float ReportDemand()
    {
        return OutputNodes.Sum(output => output.ReportDemand());
    }

    public float ReceivePacket(PowerPacket powerPacket)
    {
        if (OutputNodes.Count == 0)
        {
            return 0.0f;
        }

        float totalDemand = ReportDemand();
        float energyDistributed = 0.0f;

        PowerUtils powerUtils = Object.FindFirstObjectByType<PowerUtils>();

        foreach (var outputNode in OutputNodes.OrderBy(output => (output as PowerConsumer)?.priority ?? EPowerPriority.Unassigned))
        {
            float demand = outputNode.ReportDemand();
            if (demand == 0.0f)
            {
                continue;
            }

            float fraction = demand / totalDemand;
            float energyToSend = powerPacket.energy * fraction;

            powerUtils.QueueNumber(energyToSend, transform.position + (transform.position - outputNode.GetTransform().position) * 0.5f);

            energyDistributed += outputNode.ReceivePacket(new PowerPacket() { energy = energyToSend });
        }

        return energyDistributed;
    }

    public Transform GetTransform()
    {
        return transform;
    }
}
