using System.Collections.Generic;
using UnityEngine;

public class PowerController : MonoBehaviour
{
    public List<PowerProducer> powerProducers = new();
    public List<BatteryNode> batteries = new();
    public List<PowerConsumer> powerConsumers = new();

    public int DelayTicks = 60;
    public int TickAccum = 0;

    public void Update()
    {
        if (TickAccum > 0)
        {
            --TickAccum;
            return;
        }

        TickAccum = DelayTicks;

        foreach (var producer in powerProducers)
        {
            producer.EmitPackets();
        }

        foreach (var battery in batteries)
        {
            battery.DistributeToOutputs();
        }

        foreach (var consumer in powerConsumers)
        {
            consumer.UpdatePower();
        }
    }
}