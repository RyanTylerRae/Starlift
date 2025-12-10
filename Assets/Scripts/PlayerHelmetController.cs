#nullable enable

using System.Collections;
using UnityEditor.Animations;
using UnityEngine;

public class PlayerHelmetController : MonoBehaviour
{
    public Animator? helmetAnimator;
    public GameObject? hudObject;
    public string helmetLowerStateName = "Lower Helmet"; // Set this to match your animator state name
    private AnimatorStateInfo? cachedAnimatorStateInfo = null;

    public void Start()
    {
        StartCoroutine(WaitForHelmetAnimation());
    }

    public void Update()
    {
        cachedAnimatorStateInfo = helmetAnimator?.GetCurrentAnimatorStateInfo(0) ?? null;
    }

    public void OnEnable()
    {
        if (cachedAnimatorStateInfo != null)
        {
            helmetAnimator?.Play(cachedAnimatorStateInfo.Value.fullPathHash, 0, cachedAnimatorStateInfo.Value.normalizedTime);
            helmetAnimator?.Update(0.0f);
        }
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
