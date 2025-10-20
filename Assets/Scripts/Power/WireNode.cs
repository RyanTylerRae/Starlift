using System.Collections.Generic;
using UnityEngine;

public class WireNode : MonoBehaviour
{
    // Editor-exposed lists for manual setup
    public List<GameObject> wireObjectsToConnect = new();
    public List<GameObject> producerObjectsToConnect = new();
    public List<GameObject> consumerObjectsToConnect = new();

    // Runtime connection lists (hidden from inspector)
    [HideInInspector] public List<WireNode> connectedWires = new();
    [HideInInspector] public List<PowerProducer> connectedProducers = new();
    [HideInInspector] public List<PowerConsumer> connectedConsumers = new();

    // Track which connection IDs flow through this wire
    [HideInInspector] public HashSet<uint> activeConnectionIds = new();

    // Notify PowerController when connections change
    public void OnConnectionChanged()
    {
        // Invalidate all connections that flow through this wire
        PowerController.Instance.RebuildConnections(activeConnectionIds);
    }

    private void Start()
    {
        PowerController.Instance.RegisterWireNode(this);

        // Convert GameObject lists to component references
        connectedWires.Clear();
        connectedProducers.Clear();
        connectedConsumers.Clear();

        foreach (GameObject wireObj in wireObjectsToConnect)
        {
            if (wireObj != null)
            {
                WireNode wireNode = wireObj.GetComponent<WireNode>();
                if (wireNode != null && !connectedWires.Contains(wireNode))
                {
                    connectedWires.Add(wireNode);
                }
            }
        }

        foreach (GameObject producerObj in producerObjectsToConnect)
        {
            if (producerObj != null)
            {
                PowerProducer producer = producerObj.GetComponent<PowerProducer>();
                if (producer != null && !connectedProducers.Contains(producer))
                {
                    connectedProducers.Add(producer);
                }
            }
        }

        foreach (GameObject consumerObj in consumerObjectsToConnect)
        {
            if (consumerObj != null)
            {
                PowerConsumer consumer = consumerObj.GetComponent<PowerConsumer>();
                if (consumer != null && !connectedConsumers.Contains(consumer))
                {
                    connectedConsumers.Add(consumer);
                }
            }
        }

        // Rebuild pathways for any connected producers
        foreach (PowerProducer producer in connectedProducers)
        {
            PowerController.Instance.RebuildProducerPaths(producer);
        }
    }

    private void OnDestroy()
    {
        PowerController.Instance.UnregisterWireNode(this);

        // Invalidate all connections flowing through this wire when destroyed
        if (activeConnectionIds.Count > 0)
        {
            PowerController.Instance.RebuildConnections(activeConnectionIds);
        }
    }
}
