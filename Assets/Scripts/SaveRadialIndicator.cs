#nullable enable

using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class SaveRadialIndicator : MonoBehaviour
{
    public Image? saveRadialImage;
    private Material? saveRadialMaterial = null;

    public float shiftDuration = 1.0f;

    private const float startRotation = 0.0f;
    private const float endRotation = 3.0f;

    private int shiftId = 0;

    public void Start()
    {
        saveRadialMaterial = saveRadialImage?.material;
        saveRadialMaterial?.SetFloat("_Rotation", endRotation);
        saveRadialImage?.gameObject.SetActive(false);
    }

    public async void TriggerSave()
    {
        if (saveRadialMaterial == null || saveRadialImage == null)
        {
            return;
        }

        int currentShiftId = ++shiftId;

        saveRadialImage.gameObject.SetActive(true);
        saveRadialMaterial.SetFloat("_Rotation", startRotation);

        float elapsed = 0.0f;

        while (elapsed < shiftDuration)
        {
            if (currentShiftId != shiftId)
            {
                return;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / shiftDuration);
            saveRadialMaterial.SetFloat("_Rotation", Mathf.Lerp(startRotation, endRotation, t));

            await Task.Yield();
        }

        if (currentShiftId != shiftId)
        {
            return;
        }

        saveRadialMaterial.SetFloat("_Rotation", endRotation);
        saveRadialImage.gameObject.SetActive(false);
    }
}
