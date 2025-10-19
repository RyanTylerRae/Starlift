using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PowerProducer : MonoBehaviour
{
    public float energyOutputRate;

    // List of connection IDs representing output wire paths from this producer
    public List<uint> outputConnections = new();

    public void SendEnergy()
    {
        float remainingEnergy = energyOutputRate;

        // Collect all reachable consumers from all output connections
        List<PowerConsumer> allConsumers = new();
        foreach (uint connectionId in outputConnections)
        {
            List<PowerConsumer> consumersForConnection = PowerController.Instance.GetConsumersForConnection(connectionId);
            foreach (PowerConsumer consumer in consumersForConnection)
            {
                if (!allConsumers.Contains(consumer))
                {
                    allConsumers.Add(consumer);
                }
            }
        }

        // Sort by priority (Essential first, then others)
        allConsumers = allConsumers.OrderBy(c => c.priority).ToList();

        // Distribute energy in priority order until we run out
        foreach (PowerConsumer consumer in allConsumers)
        {
            if (remainingEnergy <= 0)
                break;

            float energyNeeded = consumer.requiredEnergy - consumer.energyReceivedThisFrame;
            float energyToSend = Mathf.Min(remainingEnergy, energyNeeded);

            if (energyToSend > 0)
            {
                consumer.ReceiveEnergy(energyToSend);
                remainingEnergy -= energyToSend;
            }
        }
    }

    private void Start()
    {
        PowerController.Instance.AddPowerProducer(this);
    }

    private void OnDestroy()
    {
        PowerController.Instance.RemovePowerProducer(this);
    }
}
