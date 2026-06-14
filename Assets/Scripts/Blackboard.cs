#nullable enable

using System.Collections.Generic;
using UnityEngine;

public enum BlackboardType
{
    Bool,
    Int,
    Float,
    String
}

public class Blackboard : MonoBehaviour
{
    private static Blackboard? instance;

    public static Blackboard Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject(nameof(Blackboard));
                instance = go.AddComponent<Blackboard>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    private static readonly Dictionary<BlackboardType, System.Type> typeMap = new()
    {
        { BlackboardType.Bool,   typeof(bool)   },
        { BlackboardType.Int,    typeof(int)    },
        { BlackboardType.Float,  typeof(float)  },
        { BlackboardType.String, typeof(string) },
    };

    private readonly Dictionary<string, BlackboardType> registry = new();
    private readonly Dictionary<string, object> values = new();
    private readonly object _lock = new();

    public void Register(string key, BlackboardType type)
    {
        lock (_lock)
        {
            registry[key] = type;
            if (!values.ContainsKey(key))
            {
                values[key] = type switch
                {
                    BlackboardType.Bool   => (object)false,
                    BlackboardType.Int    => (object)0,
                    BlackboardType.Float  => (object)0f,
                    BlackboardType.String => (object)string.Empty,
                    _                     => throw new System.ArgumentOutOfRangeException(nameof(type))
                };
            }
        }
    }

    public void Set<T>(string key, T value) where T : notnull
    {
        lock (_lock)
        {
            if (!registry.TryGetValue(key, out BlackboardType registeredType))
            {
                throw new System.InvalidOperationException($"Blackboard key '{key}' is not registered.");
            }
            if (typeMap[registeredType] != typeof(T))
            {
                throw new System.InvalidOperationException($"Blackboard key '{key}' is registered as {registeredType}, not {typeof(T).Name}.");
            }
            values[key] = value;
            Debug.Log($"[Blackboard] {key} = {value}");
        }
    }

    public T? Get<T>(string key) where T : class
    {
        lock (_lock)
        {
            if (!registry.TryGetValue(key, out BlackboardType registeredType))
            {
                throw new System.InvalidOperationException($"Blackboard key '{key}' is not registered.");
            }
            if (typeMap[registeredType] != typeof(T))
            {
                throw new System.InvalidOperationException($"Blackboard key '{key}' is registered as {registeredType}, not {typeof(T).Name}.");
            }
            if (values.TryGetValue(key, out object obj) && obj is T typed)
            {
                return typed;
            }
            return null;
        }
    }

    public bool GetValue<T>(string key, out T result) where T : struct
    {
        lock (_lock)
        {
            if (!registry.TryGetValue(key, out BlackboardType registeredType))
            {
                throw new System.InvalidOperationException($"Blackboard key '{key}' is not registered.");
            }
            if (typeMap[registeredType] != typeof(T))
            {
                throw new System.InvalidOperationException($"Blackboard key '{key}' is registered as {registeredType}, not {typeof(T).Name}.");
            }
            if (values.TryGetValue(key, out object obj) && obj is T typed)
            {
                result = typed;
                return true;
            }
        }
        result = default;
        return false;
    }

    public bool Has(string key)
    {
        lock (_lock)
        {
            return values.ContainsKey(key);
        }
    }

    public void Remove(string key)
    {
        lock (_lock)
        {
            values.Remove(key);
        }
    }
}
