#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GravityController : MonoBehaviour
{
    [Header("Zero-G Transition")]
    public float minInitialForce = 20.0f;
    public float maxInitialForce = 30.0f;
    public float minInitialTorque = 0.3f;
    public float maxInitialTorque = 0.7f;
    public float forceConeDegrees = 15.0f;

    public event Action<GravitySourceComponent?>? ActiveSourceChanged;

    private List<GravitySourceComponent> gravitySources = new List<GravitySourceComponent>();
    private Dictionary<GravitySourceComponent, GameObject> _intermediateParents = new();
    private Rigidbody? _rigidBody;
    private GravitySourceComponent? _activeSource;
    private bool hadGravityLastFrame;
    private Vector3 lastGravityDirection = Vector3.down;

    public void Start()
    {
        _rigidBody = GetComponent<Rigidbody>();
    }

    public void FixedUpdate()
    {
        if (_rigidBody != null)
        {
            Vector3 gravity = GetGravityVector();
            _rigidBody.AddForce(gravity);

            // Detect transition from gravity to zero-g
            bool hasGravityNow = gravity.sqrMagnitude > 0.01f;

            // Skip transition detection on first frame to initialize state
            if (hadGravityLastFrame && !hasGravityNow)
            {
                ApplyZeroGTransitionForces();
            }

            hadGravityLastFrame = hasGravityNow;
            lastGravityDirection = gravity.normalized;
        }
    }

    private void ApplyZeroGTransitionForces()
    {
        if (_rigidBody == null)
        {
            return;
        }

        // Ensure rigidbody is awake and can receive forces
        _rigidBody.WakeUp();

        // Calculate direction opposite to gravity
        Vector3 oppositeGravity = -lastGravityDirection;

        // Generate random direction within cone
        Vector3 randomDirection = GetRandomDirectionInCone(oppositeGravity, forceConeDegrees);

        // Apply random initial force in the random direction within the cone
        float randomForce = UnityEngine.Random.Range(minInitialForce, maxInitialForce);
        _rigidBody.AddForce(randomDirection * randomForce);

        // Apply random initial torque
        Vector3 randomTorque = new Vector3(
            UnityEngine.Random.Range(-1f, 1f),
            UnityEngine.Random.Range(-1f, 1f),
            UnityEngine.Random.Range(-1f, 1f)
        );

        float randomTorqueMagnitude = UnityEngine.Random.Range(minInitialTorque, maxInitialTorque);
        _rigidBody.AddTorque(randomTorque * randomTorqueMagnitude);
    }

    private Vector3 GetRandomDirectionInCone(Vector3 direction, float angleDegrees)
    {
        // Convert angle to radians
        float angleRadians = angleDegrees * Mathf.Deg2Rad;

        // Generate random angle within cone
        float randomAngle = UnityEngine.Random.Range(0f, angleRadians);
        float randomRotation = UnityEngine.Random.Range(0f, 2f * Mathf.PI);

        // Create perpendicular vectors to the direction
        Vector3 perpendicular1 = Vector3.Cross(direction, Vector3.up);
        if (perpendicular1.sqrMagnitude < 0.01f)
        {
            perpendicular1 = Vector3.Cross(direction, Vector3.right);
        }
        perpendicular1.Normalize();

        Vector3 perpendicular2 = Vector3.Cross(direction, perpendicular1).normalized;

        // Calculate point on cone
        float radius = Mathf.Tan(randomAngle);
        Vector3 offset = (perpendicular1 * Mathf.Cos(randomRotation) + perpendicular2 * Mathf.Sin(randomRotation)) * radius;

        return (direction + offset).normalized;
    }

    public IReadOnlyList<GravitySourceComponent> GetGravitySources() => gravitySources;

    public void AddGravitySource(GravitySourceComponent gravityComponent)
    {
        if (gravitySources.Contains(gravityComponent))
        {
            return;
        }

        gravitySources.Add(gravityComponent);

        var ip = new GameObject("GravityParent_" + gameObject.name);
        ip.transform.SetParent(gravityComponent.transform, false);
        Vector3 sourceScale = gravityComponent.transform.lossyScale;
        ip.transform.localScale = new Vector3(1f / sourceScale.x, 1f / sourceScale.y, 1f / sourceScale.z);
        _intermediateParents[gravityComponent] = ip;

        RefreshActiveParenting();
    }

    public void RemoveGravitySource(GravitySourceComponent gravityComponent)
    {
        gravitySources.Remove(gravityComponent);

        if (_intermediateParents.TryGetValue(gravityComponent, out var ip))
        {
            if (transform.parent == ip.transform)
                transform.SetParent(null, true);
            Destroy(ip);
            _intermediateParents.Remove(gravityComponent);
        }

        RefreshActiveParenting();
    }

    private void RefreshActiveParenting()
    {
        var active = GetHighestPrioritySource();
        if (active != null && _intermediateParents.TryGetValue(active, out var ip))
            transform.SetParent(ip.transform, true);
        else
            transform.SetParent(null, true);

        if (active != _activeSource)
        {
            _activeSource = active;
            ActiveSourceChanged?.Invoke(active);
        }
    }

    private GravitySourceComponent? GetHighestPrioritySource() =>
        gravitySources.Count > 0
            ? gravitySources.OrderByDescending(s => s.priority).First()
            : null;

    public Vector3 GetGravityVector()
    {
        var source = GetHighestPrioritySource();
        return source != null ? source.GetGravityVector(gameObject.transform.position) : Vector3.zero;
    }

    public GravitySourceComponent? GetActiveGravitySource() => GetHighestPrioritySource();
}
