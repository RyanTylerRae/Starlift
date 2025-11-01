#nullable enable

using UnityEngine;

public class PowerSourceTestToggle : MonoBehaviour
{
    private PowerProducer powerProducer;

    public float updateIntervalSeconds = 10.0f;
    private float lastUpdateTime = 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        powerProducer = GetComponent<PowerProducer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.time - lastUpdateTime < updateIntervalSeconds)
        {
            return;
        }

        lastUpdateTime = Time.time;

        if (powerProducer.energyOutputRate > 0.0f)
        {
            powerProducer.energyOutputRate = 0.0f;
        }
        else
        {
            powerProducer.energyOutputRate = 1.0f;
        }
    }
}
