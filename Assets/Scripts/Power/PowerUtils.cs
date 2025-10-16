using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Utility to display floating number text in world space,
/// that rises along Y-axis and fades out over time.
/// </summary>
public class PowerUtils : MonoBehaviour
{
    [Header("Settings")]
    public float FloatSpeed = 1.0f;        // Units per second upward
    public int FadeFrames = 60;            // Lifetime in frames before fully faded
    public Color TextColor = Color.yellow; // Base text color
    public float TextSize = 1.0f;          // Scale multiplier for text size
    public Font TextFont;                  // Optional: assign a font for rendering

    class FloatingNumber
    {
        public Vector3 WorldPos;
        public float Value;
        public float Opacity;
        public int FramesAlive;
        public GameObject LabelObj;
        public TextMesh Text;
    }

    private readonly List<FloatingNumber> _numbers = new();

    private Camera _mainCamera;

    void Awake()
    {
        _mainCamera = Camera.main;
    }

    /// <summary>
    /// Queues a new floating number at a given world position.
    /// </summary>
    public void QueueNumber(float number, Vector3 worldPos)
    {
        var go = new GameObject("FloatingNumber");
        var textMesh = go.AddComponent<TextMesh>();
        textMesh.text = number.ToString("0.##");
        textMesh.color = TextColor;
        textMesh.characterSize = 0.1f * TextSize;
        textMesh.fontSize = 64;
        textMesh.anchor = TextAnchor.MiddleCenter;
        if (TextFont) textMesh.font = TextFont;

        var entry = new FloatingNumber
        {
            WorldPos = worldPos,
            Value = number,
            Opacity = 1f,
            FramesAlive = 0,
            LabelObj = go,
            Text = textMesh
        };

        go.transform.position = worldPos;
        _numbers.Add(entry);
    }

    void LateUpdate()
    {
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null)
                return;
        }

        for (int i = _numbers.Count - 1; i >= 0; i--)
        {
            var n = _numbers[i];

            // Move upward
            n.WorldPos += Vector3.up * FloatSpeed * Time.deltaTime;
            n.LabelObj.transform.position = n.WorldPos;

            // Billboard toward camera
            n.LabelObj.transform.rotation = Quaternion.LookRotation(_mainCamera.transform.forward);

            // Fade opacity over lifetime
            n.FramesAlive++;
            float t = Mathf.Clamp01((float)n.FramesAlive / FadeFrames);
            n.Opacity = 1f - t;

            var c = TextColor;
            c.a = n.Opacity;
            n.Text.color = c;

            // Remove when invisible
            if (n.Opacity <= 0f)
            {
                Destroy(n.LabelObj);
                _numbers.RemoveAt(i);
            }
        }
    }
}
