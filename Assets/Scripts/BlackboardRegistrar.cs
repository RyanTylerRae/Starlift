#nullable enable

using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BlackboardEntry
{
    public string key = "";
    public BlackboardType type = BlackboardType.Bool;
    public bool defaultBool = false;
    public int defaultInt = 0;
    public float defaultFloat = 0f;
    public string defaultString = "";
}

public class BlackboardRegistrar : MonoBehaviour
{
    public List<BlackboardEntry> entries = new();

    void Awake()
    {
        foreach (BlackboardEntry entry in entries)
        {
            if (string.IsNullOrEmpty(entry.key))
            {
                continue;
            }

            Blackboard.Instance.Register(entry.key, entry.type);

            switch (entry.type)
            {
                case BlackboardType.Bool:
                    Blackboard.Instance.Set(entry.key, entry.defaultBool);
                    break;
                case BlackboardType.Int:
                    Blackboard.Instance.Set(entry.key, entry.defaultInt);
                    break;
                case BlackboardType.Float:
                    Blackboard.Instance.Set(entry.key, entry.defaultFloat);
                    break;
                case BlackboardType.String:
                    Blackboard.Instance.Set(entry.key, entry.defaultString);
                    break;
            }
        }
    }
}
