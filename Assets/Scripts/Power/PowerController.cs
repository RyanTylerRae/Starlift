using System.Collections.Generic;
using UnityEngine;

public class PowerController : Singleton<PowerController>
{
    // All registered producers and consumers
    private List<PowerProducer> powerProducers = new();
    private List<PowerConsumer> powerConsumers = new();

    // All registered wire nodes
    private List<WireNode> wireNodes = new();

    // Maps uniqueId to the list of consumers reachable from a producer
    private Dictionary<uint, List<PowerConsumer>> connectionMap = new();

    // Maps connectionId to the producer that owns it
    private Dictionary<uint, PowerProducer> connectionToProducer = new();

    public float updateIntervalSeconds = 1.0f;
    private float lastUpdateTime = 0f;

    private uint nextUniqueId = 1;

    public void Update()
    {
        if (Time.time - lastUpdateTime < updateIntervalSeconds)
        {
            return;
        }

        lastUpdateTime = Time.time;

        foreach (PowerProducer powerProducer in powerProducers)
        {
            powerProducer.SendEnergy();
        }

        foreach (PowerConsumer powerConsumer in powerConsumers)
        {
            powerConsumer.UseEnergy();
        }
    }

    public void AddPowerProducer(PowerProducer powerProducer)
    {
        if (!powerProducers.Contains(powerProducer))
        {
            powerProducers.Add(powerProducer);
        }
    }

    public void RemovePowerProducer(PowerProducer powerProducer)
    {
        powerProducers.Remove(powerProducer);

        foreach (uint connectionId in powerProducer.outputConnections)
        {
            RemoveConnectionTracking(connectionId);
        }
    }

    public void AddPowerConsumer(PowerConsumer powerConsumer)
    {
        if (!powerConsumers.Contains(powerConsumer))
        {
            powerConsumers.Add(powerConsumer);
        }
    }

    public void RemovePowerConsumer(PowerConsumer powerConsumer)
    {
        powerConsumers.Remove(powerConsumer);

        foreach (var consumerList in connectionMap.Values)
        {
            consumerList.Remove(powerConsumer);
        }
    }

    public void RegisterWireNode(WireNode wireNode)
    {
        if (!wireNodes.Contains(wireNode))
        {
            wireNodes.Add(wireNode);
        }
    }

    public void UnregisterWireNode(WireNode wireNode)
    {
        wireNodes.Remove(wireNode);
    }

    public uint GenerateConnectionId()
    {
        return nextUniqueId++;
    }

    public List<PowerConsumer> GetConsumersForConnection(uint connectionId)
    {
        if (connectionMap.TryGetValue(connectionId, out List<PowerConsumer> consumers))
        {
            return consumers;
        }
        return new List<PowerConsumer>();
    }

    // called when wire topology changes - recalculates which consumers are reachable from a connection
    public void UpdateConnectionPath(uint connectionId, List<PowerConsumer> reachableConsumers)
    {
        if (connectionMap.ContainsKey(connectionId))
        {
            connectionMap[connectionId] = new List<PowerConsumer>(reachableConsumers);
        }
        else
        {
            connectionMap.Add(connectionId, new List<PowerConsumer>(reachableConsumers));
        }
    }

    // invalidate specific connections and rebuild them from their source producers
    public void RebuildConnections(HashSet<uint> connectionIds)
    {
        // Group connections by their owning producer
        HashSet<PowerProducer> affectedProducers = new();

        foreach (uint connectionId in connectionIds)
        {
            if (connectionToProducer.TryGetValue(connectionId, out PowerProducer producer))
            {
                affectedProducers.Add(producer);
            }
        }

        foreach (PowerProducer producer in affectedProducers)
        {
            RebuildProducerPaths(producer);
        }
    }

    // rebuild all connection paths for a specific producer - alled when wire topology changes
    public void RebuildProducerPaths(PowerProducer producer)
    {
        foreach (uint connectionId in producer.outputConnections)
        {
            RemoveConnectionTracking(connectionId);
        }
        producer.outputConnections.Clear();

        List<WireNode> startWires = FindWiresConnectedToProducer(producer);

        foreach (WireNode startWire in startWires)
        {
            uint connectionId = GenerateConnectionId();
            producer.outputConnections.Add(connectionId);

            connectionToProducer[connectionId] = producer;

            List<PowerConsumer> reachableConsumers = TraverseWireNetworkAndRegister(startWire, connectionId);
            UpdateConnectionPath(connectionId, reachableConsumers);
        }
    }

    // Remove all tracking for a specific connection
    private void RemoveConnectionTracking(uint connectionId)
    {
        // Remove from connection map
        connectionMap.Remove(connectionId);

        // Remove from producer mapping
        connectionToProducer.Remove(connectionId);

        // Remove from all wires that tracked this connection
        foreach (WireNode wire in wireNodes)
        {
            wire.activeConnectionIds.Remove(connectionId);
        }
    }

    // Find all WireNodes that have this producer in their connectedProducers list
    private List<WireNode> FindWiresConnectedToProducer(PowerProducer producer)
    {
        List<WireNode> wires = new();

        // Use cached wireNodes list instead of FindObjectsByType
        foreach (WireNode wire in wireNodes)
        {
            if (wire.connectedProducers.Contains(producer))
            {
                wires.Add(wire);
            }
        }

        return wires;
    }

    // Traverse the wire network and register the connectionId with each wire visited
    private List<PowerConsumer> TraverseWireNetworkAndRegister(WireNode startWire, uint connectionId)
    {
        List<PowerConsumer> consumers = new();
        HashSet<WireNode> visited = new();
        Queue<WireNode> toVisit = new();

        toVisit.Enqueue(startWire);

        while (toVisit.Count > 0)
        {
            WireNode current = toVisit.Dequeue();

            if (visited.Contains(current))
                continue;

            visited.Add(current);

            // Register this connection with the wire
            current.activeConnectionIds.Add(connectionId);

            // Add any consumers connected to this wire
            foreach (PowerConsumer consumer in current.connectedConsumers)
            {
                if (!consumers.Contains(consumer))
                {
                    consumers.Add(consumer);
                }
            }

            // Continue traversing to connected wires
            foreach (WireNode nextWire in current.connectedWires)
            {
                if (!visited.Contains(nextWire))
                {
                    toVisit.Enqueue(nextWire);
                }
            }
        }

        return consumers;
    }
}
