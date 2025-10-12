using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GravityController : MonoBehaviour
{
    private List<GravitySourceComponent> gravitySources = new List<GravitySourceComponent>();
    private Rigidbody _rigidBody;

    public void Start()
    {
        _rigidBody = GetComponent<Rigidbody>();
    }

    public void Update()
    {
        if (_rigidBody != null)
        {
            _rigidBody.AddForce(GetGravityVector());
        }
    }

    public void AddGravitySource(GravitySourceComponent gravityComponent)
    {
        gravitySources.Add(gravityComponent);
    }

    public void RemoveGravitySource(GravitySourceComponent gravityComponent)
    {
        gravitySources.Remove(gravityComponent);
    }

    public Vector3 GetGravityVector()
    {
        Vector3 gravity = Vector3.zero;

        // only use the last gravity source added for now
        if (gravitySources.Count > 0)
        {
            gravity = gravitySources.Last().GetGravityVector(gameObject);
        }

        return gravity;
    }
}
