using System.Collections.Generic;
using UnityEngine;

public class WireNode : MonoBehaviour
{
    // Neighboring wire nodes this wire connects to
    public List<WireNode> connectedWires = new();

    // Direct connections to producers (for wires at the source)
    public List<PowerProducer> connectedProducers = new();

    // Direct connections to consumers (for wires at the destination)
    public List<PowerConsumer> connectedConsumers = new();

    // Track which connection IDs flow through this wire
    public HashSet<uint> activeConnectionIds = new();

    // Notify PowerController when connections change
    public void OnConnectionChanged()
    {
        // Invalidate all connections that flow through this wire
        PowerController.Instance.RebuildConnections(activeConnectionIds);
    }

    private void Start()
    {
        PowerController.Instance.RegisterWireNode(this);
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
