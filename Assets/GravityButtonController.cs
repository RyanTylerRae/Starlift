#nullable enable

using UnityEngine;

public class GravityButtonController : MonoBehaviour
{
    public GameObject poweredCube;
    public GameObject unpoweredCube;
    private PowerConsumer? powerConsumer;

    public GameObject? gravitySourceToControl;
    private GravitySourceComponent? gravitySource;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        powerConsumer = GetComponent<PowerConsumer>();

        gravitySource = gravitySourceToControl?.GetComponentInChildren<GravitySourceComponent>();
    }

    // Update is called once per frame
    void Update()
    {
        if (gravitySource != null)
        {
            gravitySource.isGravityEnabled = true;
        }

        if (powerConsumer != null && powerConsumer.hasPower)
        {
            poweredCube.SetActive(true);
            unpoweredCube.SetActive(false);

            if (gravitySource != null)
            {
                gravitySource.isGravityEnabled = true;
            }
        }
        else
        {
            poweredCube.SetActive(false);
            unpoweredCube.SetActive(true);

            if (gravitySource != null)
            {
                gravitySource.isGravityEnabled = false;
            }
        }
    }
}
