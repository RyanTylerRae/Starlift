#nullable enable

using System.Collections;
using UnityEngine;

public class PlayerHelmetController : MonoBehaviour
{
    public Animator? helmetAnimator;
    public GameObject? hudObject;
    public string helmetLowerStateName = "Lower Helmet"; // Set this to match your animator state name

    public void Start()
    {
        StartCoroutine(WaitForHelmetAnimation());
    }

    public void LowerHelmet()
    {
        if (helmetAnimator != null)
        {
            helmetAnimator.SetBool("isHelmetOn", true);
            StartCoroutine(WaitForHelmetAnimation());
        }
    }

    public void RaiseHelmet()
    {
        if (helmetAnimator != null)
        {
            helmetAnimator.SetBool("isHelmetOn", false);
        }

        if (hudObject != null)
        {
            hudObject.SetActive(false);
        }
    }

    private IEnumerator WaitForHelmetAnimation()
    {
        if (helmetAnimator == null) yield break;

        // Wait until we're in the helmet lowering state
        while (!helmetAnimator.GetCurrentAnimatorStateInfo(0).IsName(helmetLowerStateName))
        {
            yield return null;
        }

        // Now wait for that specific state's animation to complete
        while (helmetAnimator.GetCurrentAnimatorStateInfo(0).IsName(helmetLowerStateName) &&
               helmetAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
        {
            yield return null;
        }

        // Activate the HUD once the animation is complete
        if (hudObject != null)
        {
            hudObject.SetActive(true);
        }
    }
}
