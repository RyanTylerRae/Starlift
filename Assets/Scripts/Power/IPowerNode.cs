using UnityEngine;

public interface IPowerNode
{
    public float ReportDemand();
    public float ReceivePacket(PowerPacket powerPacket);

    public Transform GetTransform();
}