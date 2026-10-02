using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class MenuSnakeAnimation : MonoBehaviour
{
    [Header("Appearance")]
    public RectTransform headSprite; 
    public RectTransform bodySprite;
    [Min(1)] public int bodyCount = 18;
    [Min(1f)] public float partSize = 120f;
    [Min(1f)] public float spacing = 75f;

    [Header("Movement")]
    [Min(0f)] public float speed = 180f;

    // Use -90 if your sprite faces upward.
    public float rotationOffset = 0f;

    [Header("Path: 0 to 1 is inside the menu")]
    public Vector2[] points = CreateGentlePath();

    private static Vector2[] CreateGentlePath() => new Vector2[]
    {
        // Gently bowed diagonal from bottom-left to top-right.
        new Vector2(-0.30f, -0.45f),
        new Vector2(0.10f, 0.00f),
        new Vector2(0.60f, 0.50f),
        new Vector2(0.95f, 1.00f),
        new Vector2(1.30f, 1.50f),
        // Offscreen route to the bottom-right entrance.
        new Vector2(1.60f, 1.70f),
        new Vector2(1.80f, 0.50f),
        new Vector2(1.60f, -0.70f),
        // Diagonal pass from bottom-right to top-left.
        new Vector2(1.25f, -0.63f),
        new Vector2(0.95f, -0.18f),
        new Vector2(0.40f, 0.32f),
        new Vector2(0.00f, 0.77f),
        new Vector2(-0.50f, 1.22f),
        // Offscreen return to the bottom-left entrance.
        new Vector2(-0.85f, 1.60f),
        new Vector2(-1.00f, 0.50f),
        new Vector2(-0.85f, -0.70f),
        new Vector2(-0.65f, -0.85f)
    };

    [ContextMenu("Apply Gentle Menu Path")]
    private void ApplyGentleMenuPath()
    {
#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(this, "Apply Gentle Menu Path");
#endif
        points = CreateGentlePath();
#if UNITY_EDITOR
        UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(this);
        UnityEditor.EditorUtility.SetDirty(this);
#endif
        if (Application.isPlaying && area != null)
            BuildPath();
    }

    private readonly List<Vector2> samples = new List<Vector2>();
    private readonly List<float> distances = new List<float>();

    private RectTransform area;
    private RectTransform[] parts;
    private Vector2 previousSize;
    private float pathLength;
    private float headDistance;

    private void Start()
    {
        if (headSprite == null || bodySprite == null ||
            points == null || points.Length < 4)
        {
            Debug.LogWarning(
                "Menu snake needs both sprites and at least four path points.",
                this);
            enabled = false;
            return;
        }

        area = GetComponent<RectTransform>();
        Canvas.ForceUpdateCanvases();

        headSprite.gameObject.SetActive(false);
        bodySprite.gameObject.SetActive(false);

        parts = new RectTransform[bodyCount + 1];

        // Create the tail first so the head draws on top.
        for (int i = parts.Length - 1; i >= 0; i--)
        {
            RectTransform template =
            i == 0 ? headSprite : bodySprite;

            RectTransform part = Instantiate(template, area, false);

            part.name = i == 0 ? "Head" : "Body " + i;
            part.anchorMin = new Vector2(0.5f, 0.5f);
            part.anchorMax = new Vector2(0.5f, 0.5f);
            part.pivot = new Vector2(0.5f, 0.5f);
            part.localScale = Vector3.one;

            // Scale the template and its triangle together.
            float templateWidth = Mathf.Max(1f, template.rect.width);
            float sizeMultiplier = i == 0 ? 1f : 0.75f;
            part.localScale = Vector3.one *
                (partSize * sizeMultiplier / templateWidth);

            foreach (Image image in part.GetComponentsInChildren<Image>(true))
                image.raycastTarget = false;

            part.gameObject.SetActive(true);
            parts[i] = part;
        }

        BuildPath();
        PositionParts();
    }

    private Vector2 GetPoint(int index)
    {
        int count = points.Length;
        Vector2 point = points[(index + count) % count];

        // Convert normalized menu coordinates to centered UI coordinates.
        return Vector2.Scale(point - Vector2.one * 0.5f, area.rect.size);
    }

    private void BuildPath()
    {
        samples.Clear();
        distances.Clear();
        pathLength = 0f;
        previousSize = area.rect.size;

        const int stepsPerCurve = 80;

        for (int i = 0; i < points.Length; i++)
        {
            Vector2 a = GetPoint(i - 1);
            Vector2 b = GetPoint(i);
            Vector2 c = GetPoint(i + 1);
            Vector2 d = GetPoint(i + 2);

            for (int step = 0; step < stepsPerCurve; step++)
            {
                float t = step / (float)stepsPerCurve;
                Vector2 position = EvaluateCurve(a, b, c, d, t);

                AddSample(position);
            }
        }

        // Close the loop.
        AddSample(samples[0]);
    }

    private static Vector2 EvaluateCurve(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float t)  // Catmull-Rom spline interpolation
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * (2f * b + (-a + c) * t +
            (2f * a - 5f * b + 4f * c - d) * t2 +
            (-a + 3f * b - 3f * c + d) * t3);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Keep this overlay in Scene view, even if Game-view Gizmos are enabled.
        if (Camera.current == null || Camera.current.cameraType != CameraType.SceneView ||
            points == null || points.Length < 4)
            return;

        RectTransform previewArea = GetComponent<RectTransform>();
        Rect rect = previewArea.rect;
        Vector3 ToWorld(Vector2 point) => previewArea.TransformPoint(
            (Vector3)(rect.center + Vector2.Scale(point - Vector2.one * 0.5f, rect.size)));

        Color previousColor = UnityEditor.Handles.color;
        try
        {
            int count = points.Length;
            var curve = new Vector3[81];
            for (int i = 0; i < count; i++)
            {
                UnityEditor.Handles.color = Color.cyan;
                for (int step = 0; step <= 80; step++)
                    curve[step] = ToWorld(EvaluateCurve(
                        points[(i + count - 1) % count], points[i],
                        points[(i + 1) % count], points[(i + 2) % count], step / 80f));

                UnityEditor.Handles.DrawAAPolyLine(3f, curve);
                Vector3 position = ToWorld(points[i]);
                float size = UnityEditor.HandleUtility.GetHandleSize(position) * 0.06f;
                UnityEditor.Handles.color = Color.yellow;
                UnityEditor.Handles.SphereHandleCap(0, position, Quaternion.identity,
                    size, EventType.Repaint);
                UnityEditor.Handles.Label(position + previewArea.up * size,
                    $"Point {i} ({points[i].x:0.00}, {points[i].y:0.00})",
                    UnityEditor.EditorStyles.whiteLabel);
            }
        }
        finally
        {
            UnityEditor.Handles.color = previousColor;
        }
    }
#endif

    private void AddSample(Vector2 position)
    {
        if (samples.Count > 0)
            pathLength += Vector2.Distance(
                samples[samples.Count - 1], position);

        samples.Add(position);
        distances.Add(pathLength);
    }

    private Vector2 PositionAt(float distance)
    {
        distance = Mathf.Repeat(distance, pathLength);

        int index = distances.BinarySearch(distance);
        if (index < 0)
            index = ~index;

        index = Mathf.Clamp(index, 1, samples.Count - 1);

        float t = Mathf.InverseLerp(
            distances[index - 1], distances[index], distance);

        return Vector2.Lerp(samples[index - 1], samples[index], t);
    }

    private void Update()
    {
        if (area.rect.size != previousSize)
            BuildPath();

        if (pathLength <= 0.01f)
            return;

        headDistance = Mathf.Repeat(
            headDistance + speed * Time.unscaledDeltaTime, pathLength);

        PositionParts();
    }

    private void PositionParts()
    {
        if (pathLength <= 0.01f)
            return;

        for (int i = 0; i < parts.Length; i++)
        {
            float distance = headDistance - i * spacing;
            Vector2 position = PositionAt(distance);
            Vector2 direction =
                PositionAt(distance + 2f) - PositionAt(distance - 2f);

            parts[i].anchoredPosition = position;

            float angle = Mathf.Atan2(direction.y, direction.x)
                * Mathf.Rad2Deg;

            parts[i].localRotation = Quaternion.Euler(
                0f, 0f, angle + rotationOffset);
        }
    }
}

