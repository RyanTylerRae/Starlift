#nullable enable

using System.Collections.Generic;
using UnityEngine;

public class OxygenBarWidget : MonoBehaviour
{
    private struct OxygenTankSegment
    {
        public GameObject root;
        public Material foreground;
        public Material? laggy;
    }

    [Header("Segment Generation")]
    public GameObject? oxygenBarTemplate;
    public float tankSegmentBuffer = 0.05f;
    public float minSegmentWidth = 0.02f;

    [Header("Laggy Oxygen Bar")]
    public float laggyOxygenSpeed;

    private readonly List<OxygenTankSegment> segments = new List<OxygenTankSegment>();

    private Vector3 originalLocalPosition;
    private Vector3 originalLocalScale;
    private float meshWidth = 1f;

    private int generatedTankCount = -1;
    private int lastKnownLiveTankCount = -1;
    private int activeSegmentIndex = 0;

    private GameObject? player = null;

    private bool wasBurningOxygen = false;
    private float laggyOxygenProgress = 0f;

    private bool wasGodModeOxygenDisabled = false;

    public void Awake()
    {
        if (oxygenBarTemplate == null)
        {
            return;
        }

        originalLocalPosition = oxygenBarTemplate.transform.localPosition;
        originalLocalScale = oxygenBarTemplate.transform.localScale;

        // localScale is a multiplier on the mesh's own size, not a real distance;
        // position offsets derived from scale need converting via the mesh's true local width
        if (oxygenBarTemplate.TryGetComponent(out MeshFilter meshFilter) && meshFilter.sharedMesh != null)
        {
            meshWidth = meshFilter.sharedMesh.bounds.size.x;
        }

        oxygenBarTemplate.SetActive(false);
    }

    public void OnDestroy()
    {
        DestroySegments();
    }

    private void DestroySegments()
    {
        foreach (OxygenTankSegment segment in segments)
        {
            if (segment.foreground != null)
            {
                Destroy(segment.foreground);
            }

            if (segment.laggy != null)
            {
                Destroy(segment.laggy);
            }

            if (segment.root != null)
            {
                Destroy(segment.root);
            }
        }

        segments.Clear();
    }

    private void GenerateSegments(int tankCount)
    {
        if (oxygenBarTemplate == null)
        {
            return;
        }

        DestroySegments();

        tankCount = Mathf.Max(tankCount, 1);

        float segmentWidth = (originalLocalScale.x - tankSegmentBuffer * (tankCount - 1)) / tankCount;
        if (segmentWidth < minSegmentWidth)
        {
            Debug.LogWarning($"OxygenBarWidget: tankSegmentBuffer is too large for {tankCount} tanks; clamping segment width to {minSegmentWidth}.");
            segmentWidth = minSegmentWidth;
        }

        // segmentWidth/tankSegmentBuffer are in scale units; convert to real position-space
        // distances via meshWidth before using them as localPosition offsets
        float scaleOffsetFromCenter = -originalLocalScale.x / 2f + segmentWidth / 2f;
        float startX = originalLocalPosition.x + meshWidth * scaleOffsetFromCenter;
        float stepX = meshWidth * (segmentWidth + tankSegmentBuffer);
        int initialActiveIndex = tankCount - 1;

        for (int i = 0; i < tankCount; ++i)
        {
            GameObject clone = Instantiate(oxygenBarTemplate, oxygenBarTemplate.transform.parent);
            clone.name = $"OxygenBarSegment_{i}";
            clone.SetActive(true);

            clone.transform.localPosition = new Vector3(
                startX + i * stepX,
                originalLocalPosition.y,
                originalLocalPosition.z);
            clone.transform.localScale = new Vector3(segmentWidth, originalLocalScale.y, originalLocalScale.z);

            MeshRenderer foregroundRenderer = clone.GetComponent<MeshRenderer>();
            Transform laggyTransform = clone.transform.Find("OxygenBarLaggy");
            MeshRenderer? laggyRenderer = laggyTransform != null ? laggyTransform.GetComponent<MeshRenderer>() : null;

            // the shared ProgressBar shader is built on ShaderGraph's Canvas sub-target, which
            // doesn't reliably depth-sort against other geometry; force draw order explicitly
            // so segments always render in front of the (separately parented) background
            foregroundRenderer.sortingOrder = 1;
            if (laggyRenderer != null)
            {
                laggyRenderer.sortingOrder = 1;
            }

            Material foregroundMaterial = foregroundRenderer.material;
            Material? laggyMaterial = laggyRenderer != null ? laggyRenderer.material : null;

            float restingProgress = i < initialActiveIndex ? 1f : 0f;
            foregroundMaterial.SetFloat("_Progress", restingProgress);
            laggyMaterial?.SetFloat("_Progress", restingProgress);

            segments.Add(new OxygenTankSegment
            {
                root = clone,
                foreground = foregroundMaterial,
                laggy = laggyMaterial,
            });
        }

        generatedTankCount = tankCount;
        activeSegmentIndex = Mathf.Clamp(initialActiveIndex, 0, segments.Count - 1);
        lastKnownLiveTankCount = tankCount;
    }

    public void SetOxygenHudEnabled(bool enabled)
    {
        foreach (OxygenTankSegment segment in segments)
        {
            segment.root.SetActive(enabled);
        }
    }

    // Update is called once per frame
    public void Update()
    {
        int maxTankCount = GameState.Instance.saveData.maxOxygenTankCount;
        if (maxTankCount != generatedTankCount)
        {
            GenerateSegments(maxTankCount);
        }

        if (segments.Count == 0)
        {
            return;
        }

        if (player == null)
        {
            player = StarliftStatics.FindPlayer();
            if (player == null)
            {
                return;
            }
        }

        if (!player.TryGetComponent(out FirstPersonController playerController))
        {
            return;
        }

        if (!player.TryGetComponent(out Modifiers modifiers))
        {
            return;
        }

        if (!player.TryGetComponent(out OxygenSystem oxygenSystem))
        {
            return;
        }

        // hide/show the whole oxygen bar when God Mode is toggled
        if (PlayerSettings.GodModeOxygenDisabled != wasGodModeOxygenDisabled)
        {
            SetOxygenHudEnabled(!PlayerSettings.GodModeOxygenDisabled);
            wasGodModeOxygenDisabled = PlayerSettings.GodModeOxygenDisabled;
        }

        // detect tank transitions: only the segment(s) that changed state get a one-time snap
        int liveTankCount = oxygenSystem.CurrentTankCount;
        if (liveTankCount != lastKnownLiveTankCount)
        {
            if (liveTankCount < lastKnownLiveTankCount)
            {
                int vacatedFrom = Mathf.Clamp(liveTankCount - 1, 0, segments.Count - 1);
                int vacatedTo = Mathf.Clamp(lastKnownLiveTankCount - 1, 0, segments.Count - 1);
                for (int i = vacatedFrom + 1; i <= vacatedTo; ++i)
                {
                    segments[i].foreground.SetFloat("_Progress", 0f);
                    segments[i].laggy?.SetFloat("_Progress", 0f);
                }
            }
            else
            {
                foreach (OxygenTankSegment segment in segments)
                {
                    segment.foreground.SetFloat("_Progress", 1f);
                    segment.laggy?.SetFloat("_Progress", 1f);
                }
            }

            laggyOxygenProgress = modifiers.Get(ModifierType.Oxygen);
            wasBurningOxygen = playerController.OxygenBurnRate > 0.0f;

            activeSegmentIndex = Mathf.Clamp(liveTankCount - 1, 0, segments.Count - 1);
            lastKnownLiveTankCount = liveTankCount;
        }

        // handle oxygen burn logic for the laggy progress bar
        if (!wasBurningOxygen && playerController.OxygenBurnRate > 0.0f)
        {
            laggyOxygenProgress = modifiers.Get(ModifierType.Oxygen);
        }

        if (playerController.OxygenBurnRate <= 0.0f)
        {
            laggyOxygenProgress = Mathf.Lerp(laggyOxygenProgress, modifiers.Get(ModifierType.Oxygen), Time.deltaTime * laggyOxygenSpeed);
        }

        // ensure that laggy oxygen progress never dips below the normal oxygen bar
        laggyOxygenProgress = Mathf.Max(laggyOxygenProgress, modifiers.Get(ModifierType.Oxygen));

        wasBurningOxygen = playerController.OxygenBurnRate > 0.0f;

        // only the segment currently being expended gets its material value updated
        OxygenTankSegment activeSegment = segments[activeSegmentIndex];
        activeSegment.foreground.SetFloat("_Progress", modifiers.Get(ModifierType.Oxygen) / modifiers.GetMax(ModifierType.Oxygen));
        activeSegment.laggy?.SetFloat("_Progress", laggyOxygenProgress / modifiers.GetMax(ModifierType.Oxygen));
    }
}
