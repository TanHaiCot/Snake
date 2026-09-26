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
        // A shallow lower arc, travelling left to right.
        new Vector2(-0.45f, 0.25f),
        new Vector2( 0.00f, 0.30f),
        new Vector2( 0.50f, 0.40f),
        new Vector2( 1.00f, 0.30f),
        new Vector2( 1.45f, 0.25f),
        // Turn around well beyond the right edge.
        new Vector2( 1.85f, 0.25f),
        new Vector2( 2.00f, 0.50f),
        new Vector2( 1.85f, 0.80f),
        // A shallow upper arc, travelling right to left.
        new Vector2( 1.45f, 0.80f),
        new Vector2( 1.00f, 0.75f),
        new Vector2( 0.50f, 0.65f),
        new Vector2( 0.00f, 0.75f),
        new Vector2(-0.45f, 0.80f),
        // Turn around well beyond the left edge.
        new Vector2(-0.85f, 0.80f),
        new Vector2(-1.00f, 0.50f),
        new Vector2(-0.85f, 0.25f)
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
            float sizeMultiplier = i == 0 ? 1f : 0.8f;
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
                float t2 = t * t;
                float t3 = t2 * t;

                // Smooth curve through b and c.
                Vector2 position = 0.5f * (
                    2f * b +
                    (-a + c) * t +
                    (2f * a - 5f * b + 4f * c - d) * t2 +
                    (-a + 3f * b - 3f * c + d) * t3);

                AddSample(position);
            }
        }

        // Close the loop.
        AddSample(samples[0]);
    }

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

