#nullable enable

using TMPro;
using UnityEngine;

/// <summary>
/// Shows the audiolog AudiologManager is playing on the 3D HUD: a boxed waveform that scrolls right to
/// left, a progress bar under it, then the log's title and speaker. Built entirely in code around this
/// object's origin (the centre of the waveform box) on this object's layer - place it under LookRoot on
/// the 3D HUD camera, facing the camera. Hidden while nothing is playing.
/// The waveform follows AudiologManager's meter level when the Wwise meter is set up, and falls back to
/// procedural noise while a log plays when it isn't.
/// </summary>
public class AudiologHUD : MonoBehaviour
{
    [Header("Layout")]
    public float boxWidth = 4.0f;
    public float boxHeight = 1.0f;
    public float frameWidth = 0.04f;

    // space between the frame and the waveform inside it
    public float boxPadding = 0.1f;

    public float barHeight = 0.2f;

    // space between the bottom of the box and the top of the bar
    public float barGap = 0.06f;

    // space between the bottom of the bar and the title, and between the title and the speaker
    public float textGap = 0.04f;

    public float titleFontSize = 2.0f;
    public float speakerFontSize = 2.0f;
    public Color titleColor = Color.white;
    public Color speakerColor = Color.white;

    // leave empty to use TMP's default font
    public TMP_FontAsset? font;

    [Header("Waveform")]
    public float waveformWidth = 0.02f;

    // how many samples fit across the box - more makes the line scroll by in finer detail
    public int sampleCount = 96;
    public float samplesPerSecond = 30.0f;

    // only used when the Wwise meter isn't set up: how fast the stand-in noise changes
    public float fallbackNoiseSpeed = 8.0f;

    [Header("Materials")]
    // used for both the frame and the waveform line
    public Material? lineMaterial;
    public Material? barBackgroundMaterial;
    public Material? barFillMaterial;

    [Header("Visibility")]
    // how long the widget stays up after a log finishes, so the full bar is seen
    public float hideDelay = 0.75f;

    private GameObject? visuals;
    private LineRenderer? waveformLine;
    private TextMeshPro? titleText;
    private TextMeshPro? speakerText;
    private Material? fillMaterial;

    private AudiologComponent? shownLog;
    private float[] samples = System.Array.Empty<float>();
    private int newestSampleIndex = 0;
    private float sampleTimer = 0.0f;
    private float hideTimer = 0.0f;

    public void Awake()
    {
        Build();
    }

    public void OnDestroy()
    {
        if (fillMaterial != null)
        {
            Destroy(fillMaterial);
        }
    }

    private void Build()
    {
        int layer = gameObject.layer;

        visuals = HUDBuilder.CreateChild("Visuals", transform, layer);
        visuals.SetActive(false);

        float halfWidth = boxWidth * 0.5f;
        float halfHeight = boxHeight * 0.5f;

        LineRenderer frameLine = HUDBuilder.CreateLine("Frame", visuals.transform, layer, frameWidth, lineMaterial, 2);
        frameLine.loop = true;
        frameLine.positionCount = 4;
        frameLine.SetPositions(new Vector3[]
        {
            new Vector3(-halfWidth, halfHeight, 0.0f),
            new Vector3(halfWidth, halfHeight, 0.0f),
            new Vector3(halfWidth, -halfHeight, 0.0f),
            new Vector3(-halfWidth, -halfHeight, 0.0f),
        });

        waveformLine = HUDBuilder.CreateLine("Waveform", visuals.transform, layer, waveformWidth, lineMaterial, 2);

        // the bar spans the box's full width, barGap below it
        GameObject bar = HUDBuilder.CreateChild("Bar", visuals.transform, layer);
        float barCenterY = -halfHeight - barGap - barHeight * 0.5f;
        bar.transform.localPosition = new Vector3(0.0f, barCenterY, 0.0f);
        Vector2 barSize = new Vector2(boxWidth, barHeight);

        if (barBackgroundMaterial != null)
        {
            HUDBuilder.CreateBarQuad("Background", bar.transform, barSize, 0.0f, barBackgroundMaterial, 0);
        }

        // the ProgressBar shader doesn't reliably depth-sort, so draw order is forced with sortingOrder
        // as well as a small step toward the camera
        MeshRenderer fillRenderer = HUDBuilder.CreateBarQuad("Fill", bar.transform, barSize, -0.001f, barFillMaterial, 1);
        if (barFillMaterial != null)
        {
            fillMaterial = fillRenderer.material;
        }

        // title and speaker hang from the bar's left edge; the speaker is moved under the title once
        // the title's height is known
        float barBottomY = barCenterY - barHeight * 0.5f;
        Vector2 textSize = new Vector2(boxWidth, barHeight);

        titleText = HUDBuilder.CreateText("Title", visuals.transform, layer, font, titleFontSize, titleColor,
            TextAlignmentOptions.TopLeft, new Vector2(0.0f, 1.0f), textSize, new Vector3(-halfWidth, barBottomY - textGap, -0.002f));
        titleText.text = string.Empty;

        speakerText = HUDBuilder.CreateText("Speaker", visuals.transform, layer, font, speakerFontSize, speakerColor,
            TextAlignmentOptions.TopLeft, new Vector2(0.0f, 1.0f), textSize, new Vector3(-halfWidth, barBottomY - textGap, -0.002f));
        speakerText.text = string.Empty;

        samples = new float[Mathf.Max(sampleCount, 2)];
        waveformLine.positionCount = samples.Length + 1;
    }

    public void Update()
    {
        if (visuals == null)
        {
            return;
        }

        AudiologManager? manager = AudiologManager.Instance;
        AudiologComponent? activeLog = manager != null ? manager.ActiveLog : null;

        // hideTimer only runs after a log finishes, so this also catches the same log replayed before hiding
        if (activeLog != null && (activeLog != shownLog || hideTimer > 0.0f))
        {
            ShowLog(activeLog);
        }

        bool isPlaying = activeLog != null;
        if (!isPlaying)
        {
            if (shownLog == null)
            {
                return;
            }

            // the log finished: hold with a full bar and a flattening line, then hide
            hideTimer += Time.deltaTime;
            if (hideTimer >= hideDelay)
            {
                shownLog = null;
                visuals.SetActive(false);
                return;
            }
        }

        float level = isPlaying && manager != null ? GetLevel(manager) : 0.0f;
        UpdateSamples(level);
        UpdateWaveformLine(level);
        SetProgress(isPlaying && manager != null ? manager.Progress : 1.0f);
    }

    private void ShowLog(AudiologComponent log)
    {
        shownLog = log;
        hideTimer = 0.0f;
        sampleTimer = 0.0f;
        System.Array.Clear(samples, 0, samples.Length);

        // active before measuring the title, since TMP can't size text on an inactive object
        visuals?.SetActive(true);

        // resolved once per log rather than every frame, since a missing key logs a warning
        SetText(titleText, log.titleKey);
        SetText(speakerText, log.speakerKey);

        if (titleText != null && speakerText != null)
        {
            Vector3 speakerPosition = titleText.rectTransform.localPosition;
            if (titleText.enabled)
            {
                speakerPosition.y -= titleText.preferredHeight + textGap;
            }
            speakerText.rectTransform.localPosition = speakerPosition;
        }

        SetProgress(0.0f);
    }

    private static void SetText(TextMeshPro? text, string key)
    {
        if (text == null)
        {
            return;
        }

        text.text = string.IsNullOrEmpty(key) ? string.Empty : LocalizationManager.Instance?.GetTranslation(key) ?? key;
        text.enabled = !string.IsNullOrEmpty(text.text);
    }

    private void SetProgress(float progress)
    {
        if (fillMaterial != null)
        {
            fillMaterial.SetFloat("_Progress", Mathf.Clamp01(progress));
        }
    }

    private float GetLevel(AudiologManager manager)
    {
        if (manager.TryGetMeterLevel(out float meterLevel))
        {
            return meterLevel;
        }

        // stand-in for the meter: a slow swell, like phrases of speech, multiplied by faster detail
        float time = Time.time * fallbackNoiseSpeed;
        float swell = Mathf.PerlinNoise(time * 0.15f, 0.37f);
        float detail = Mathf.PerlinNoise(time, 4.21f);
        return Mathf.Clamp01(detail * detail * (0.4f + swell));
    }

    // records a sample at samplesPerSecond into a ring buffer, oldest overwritten first
    private void UpdateSamples(float level)
    {
        float sampleInterval = 1.0f / Mathf.Max(samplesPerSecond, 0.01f);
        sampleTimer += Time.deltaTime;

        while (sampleTimer >= sampleInterval)
        {
            sampleTimer -= sampleInterval;
            newestSampleIndex = (newestSampleIndex + 1) % samples.Length;
            samples[newestSampleIndex] = level;
        }
    }

    // the newest sample sits a fraction of a step in from the right edge and the current level fills the
    // gap, so the line scrolls smoothly between samples instead of jumping a step at a time
    private void UpdateWaveformLine(float liveLevel)
    {
        if (waveformLine == null)
        {
            return;
        }

        float innerLeft = -boxWidth * 0.5f + boxPadding;
        float innerRight = boxWidth * 0.5f - boxPadding;
        float innerBottom = -boxHeight * 0.5f + boxPadding;
        float innerHeight = boxHeight - boxPadding * 2.0f;

        int count = samples.Length;
        float step = (innerRight - innerLeft) / (count - 1);
        float sampleInterval = 1.0f / Mathf.Max(samplesPerSecond, 0.01f);
        float scrollOffset = Mathf.Clamp01(sampleTimer / sampleInterval) * step;

        for (int i = 0; i < count; ++i)
        {
            // i = 0 is the oldest sample
            int sampleIndex = (newestSampleIndex + 1 + i) % count;
            float x = innerRight - (count - 1 - i) * step - scrollOffset;
            float y = innerBottom + samples[sampleIndex] * innerHeight;
            waveformLine.SetPosition(i, new Vector3(x, y, -0.002f));
        }

        // the oldest point has scrolled past the left edge - pull it back onto the edge along the line
        Vector3 oldest = waveformLine.GetPosition(0);
        Vector3 nextOldest = waveformLine.GetPosition(1);
        if (oldest.x < innerLeft && nextOldest.x > oldest.x)
        {
            float t = (innerLeft - oldest.x) / (nextOldest.x - oldest.x);
            waveformLine.SetPosition(0, Vector3.Lerp(oldest, nextOldest, t));
        }

        waveformLine.SetPosition(count, new Vector3(innerRight, innerBottom + liveLevel * innerHeight, -0.002f));
    }
}
