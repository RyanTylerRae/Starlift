using System.Collections.Generic;
using UnityEngine;

public class PowerProducer : MonoBehaviour, IPowerNode
{
    public float powerRate;
    public List<IPowerNode> outputNodes = new();

    public void Start()
    {
        outputNodes.Add(Object.FindAnyObjectByType<PowerConsumer>());
    }

    public float ReportDemand()
    {
        return 0.0f;
    }

    public float ReceivePacket(PowerPacket powerPacket)
    {
        return 0.0f;
    }

    public void EmitPackets()
    {
        if (outputNodes.Count == 0)
        {
            return;
        }

        float power = powerRate / outputNodes.Count;

        PowerUtils powerUtils = Object.FindFirstObjectByType<PowerUtils>();

        foreach (var node in outputNodes)
        {
            node.ReceivePacket(new PowerPacket() { energy = power });

            powerUtils.QueueNumber(power, transform.position + ((transform.position - node.GetTransform().position) * 0.5f));
        }
    }

    public Transform GetTransform()
    {
        return transform;
    }
}
