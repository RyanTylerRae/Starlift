#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TriggerVolume : MonoBehaviour
{
    public enum Filter
    {
        Player,
        Any
    }

    public enum Timing
    {
        Enter,
        Exit,
        Stay
    }

    [Serializable]
    public class TriggerEvent
    {
        public Timing timing = Timing.Enter;
        public float stayDuration = 1.0f;
        public bool triggerOnce = true;
        public UnityEvent<GameObject> onTriggered = new();

        private bool hasTriggered = false;

        public void TryInvoke(GameObject target)
        {
            if (triggerOnce && hasTriggered)
            {
                return;
            }

            hasTriggered = true;
            onTriggered.Invoke(target);
        }
    }

    private class Occupant
    {
        public float enterTime = 0.0f;
        public int colliderCount = 1;
        public HashSet<TriggerEvent> firedStayEvents = new();
    }

    public Filter filter = Filter.Player;
    public List<TriggerEvent> events = new();

    private Dictionary<GameObject, Occupant> occupants = new();
    private List<GameObject> destroyedOccupants = new();
    private List<(TriggerEvent, GameObject)> pendingStayEvents = new();

    private void OnDisable()
    {
        occupants.Clear();
    }

    private void Update()
    {
        if (occupants.Count == 0)
        {
            return;
        }

        RemoveDestroyedOccupants();

        foreach (KeyValuePair<GameObject, Occupant> pair in occupants)
        {
            float timeInside = Time.time - pair.Value.enterTime;

            foreach (TriggerEvent triggerEvent in events)
            {
                if (triggerEvent.timing != Timing.Stay || timeInside < triggerEvent.stayDuration)
                {
                    continue;
                }

                if (pair.Value.firedStayEvents.Add(triggerEvent))
                {
                    pendingStayEvents.Add((triggerEvent, pair.Key));
                }
            }
        }

        foreach ((TriggerEvent triggerEvent, GameObject target) in pendingStayEvents)
        {
            triggerEvent.TryInvoke(target);
        }

        pendingStayEvents.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!enabled)
        {
            return;
        }

        GameObject target = ResolveTarget(other);
        if (!PassesFilter(target))
        {
            return;
        }

        if (occupants.TryGetValue(target, out Occupant occupant))
        {
            occupant.colliderCount++;
            return;
        }

        occupants.Add(target, new Occupant { enterTime = Time.time });
        Fire(Timing.Enter, target);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!enabled)
        {
            return;
        }

        GameObject target = ResolveTarget(other);
        if (!occupants.TryGetValue(target, out Occupant occupant))
        {
            return;
        }

        occupant.colliderCount--;
        if (occupant.colliderCount > 0)
        {
            return;
        }

        occupants.Remove(target);
        Fire(Timing.Exit, target);
    }

    private GameObject ResolveTarget(Collider other)
    {
        if (other.attachedRigidbody != null)
        {
            return other.attachedRigidbody.gameObject;
        }

        return other.gameObject;
    }

    private bool PassesFilter(GameObject target)
    {
        switch (filter)
        {
            case Filter.Player:
                return target.TryGetComponent(out FirstPersonController _);
            default:
                return true;
        }
    }

    private void Fire(Timing timing, GameObject target)
    {
        foreach (TriggerEvent triggerEvent in events)
        {
            if (triggerEvent.timing == timing)
            {
                triggerEvent.TryInvoke(target);
            }
        }
    }

    private void RemoveDestroyedOccupants()
    {
        foreach (GameObject occupantObject in occupants.Keys)
        {
            if (occupantObject == null)
            {
                destroyedOccupants.Add(occupantObject!);
            }
        }

        foreach (GameObject destroyedObject in destroyedOccupants)
        {
            occupants.Remove(destroyedObject);
        }

        destroyedOccupants.Clear();
    }
}
