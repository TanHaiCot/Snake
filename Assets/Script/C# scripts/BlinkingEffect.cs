using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BlinkingEffect : MonoBehaviour
{
    [SerializeField] private Image blackOutImage;

    private float visibleDuration = 1f;
    private float blackoutDuration = 2f;
    private float fadeDuration = 1f;

    private void Awake()
    {
        if (blackOutImage == null)
        {
            Debug.LogError("BlinkingEffect requires an Image component on the same GameObject");
            enabled = false;
            return;
        }
        SetAlpha(0f); // Start invisible

        StartCoroutine(blinkingCoroutine());
    }

    private IEnumerator blinkingCoroutine()
    {
        while (true)
        {
            // Stay visible
            yield return new WaitForSeconds(visibleDuration);

            // Fade to invisible
            yield return StartCoroutine(FadeCroutine(0f, 1f, fadeDuration));

            // Stay invisible
            yield return new WaitForSeconds(blackoutDuration);

            // Fade to visible
            yield return StartCoroutine(FadeCroutine(1f, 0f, fadeDuration));
        }
    }


    private IEnumerator FadeCroutine(float startAlpha, float endAlpha, float duration)
    {
        float elapsedTime = 0f;
        float alphaPercentage = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            alphaPercentage = Mathf.Clamp01(elapsedTime / duration);
            float alpha = Mathf.Lerp(startAlpha, endAlpha, alphaPercentage);
            SetAlpha(alpha);
            yield return null;
        }
        SetAlpha(endAlpha);
    }

    private void SetAlpha(float alpha)
    {
        Color color = blackOutImage.color;  
        color.a = alpha;
        blackOutImage.color = color;
    }
}
