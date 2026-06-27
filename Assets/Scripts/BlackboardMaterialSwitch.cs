#nullable enable

using UnityEngine;

public class BlackboardMaterialSwitch : MonoBehaviour
{
    public string key = "";
    public Material? trueMaterial;
    public Material? falseMaterial;

    private Renderer? targetRenderer = null;
    private bool lastValue = false;

    void Awake()
    {
        targetRenderer = GetComponent<Renderer>();
    }

    void Start()
    {
        if (string.IsNullOrEmpty(key) || !Blackboard.Instance.Has(key))
        {
            return;
        }

        Blackboard.Instance.GetValue<bool>(key, out bool value);
        lastValue = value;
        ApplyMaterial(value);
    }

    void Update()
    {
        if (string.IsNullOrEmpty(key) || !Blackboard.Instance.Has(key))
        {
            return;
        }

        if (!Blackboard.Instance.GetValue<bool>(key, out bool value) || value == lastValue)
        {
            return;
        }

        lastValue = value;
        ApplyMaterial(value);
    }

    void ApplyMaterial(bool value)
    {
        if (targetRenderer == null)
        {
            return;
        }

        targetRenderer.sharedMaterial = value ? trueMaterial : falseMaterial;
    }
}
