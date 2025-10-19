using UnityEngine;

public class SingletonManager : MonoBehaviour
{
    public void Start()
    {
        _ = PowerController.Instance;
    }

    public void Update()
    {
        PowerController.Instance.Update();
    }
}
