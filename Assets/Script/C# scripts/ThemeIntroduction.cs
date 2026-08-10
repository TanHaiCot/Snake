using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ThemeIntroduction : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] GameObject introductionPanel;
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] TMP_Text themeTitleText;
    [SerializeField] Image themeImage;

    [Header("Theme Information")]
    [SerializeField] LevelData levelData;
    [SerializeField] AudioClip introSound;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float initialBlackDuration = 0.5f;
    [SerializeField, Min(0f)] private float fadeInDuration = 2f;
    [SerializeField, Min(0f)] private float displayDuration = 2f;
    [SerializeField, Min(0f)] private float fadeOutDuration = 2f;
    [SerializeField, Min(0f)] private float finalBlackDuration = 0.5f;
    [SerializeField, Min(0f)] private float waitingTimeBeforeStart = 1f; 



    private float previousTimeScale;
    private bool pausedByIntroduction;

    private void Awake()
    {
        if(!ShouldShowIntroduction())
        {
            HideImmediately(); 
            return; 
        }
        
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        pausedByIntroduction = true;

        introductionPanel.SetActive(true);

        // Ensure the introduction draws above every other UI element.
        introductionPanel.transform.SetAsLastSibling();

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

        themeImage.color = Color.black;

        themeTitleText.alpha = 0f;
        themeTitleText.text = levelData.themeIntroductionTitle;

        Canvas.ForceUpdateCanvases();

    }

    private IEnumerator Start()
    {
        if (!ShouldShowIntroduction())
            yield break;

        yield return new WaitForSecondsRealtime(initialBlackDuration);
        
        if (introSound != null)
            AudioManager.Instance?.playSFX(introSound);

        // Picture changes from black tint to its full colors.
        // Text fades in at the same time.
        yield return FadeTheme(
            Color.black,
            Color.white,
            0f,
            1f,
            fadeInDuration
        );

        yield return new WaitForSecondsRealtime(displayDuration);

        // Picture and text fade back to black.
        yield return FadeTheme(
            Color.white,
            Color.black,
            1f,
            0f,
            fadeOutDuration
        );

        yield return new WaitForSecondsRealtime(finalBlackDuration);

        //PlayerProgress progress = PlayerProgress.EnsureInstance();
        //progress.MarkThemeIntroductionSeen(levelData.themeIntroductionId);

        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        
        introductionPanel.SetActive(false);
        yield return new WaitForSecondsRealtime(waitingTimeBeforeStart);

        ResumeGame();
    }

    private bool ShouldShowIntroduction()
    {
        if (levelData == null || introductionPanel == null || canvasGroup == null || themeTitleText == null)
            return false;

        //return !PlayerProgress.EnsureInstance().HasSeenThemeIntroduction(levelData.themeIntroductionId);
        return true; 
    }
    private IEnumerator FadeTheme(
    Color imageStartColor,
    Color imageEndColor,
    float textStartAlpha,
    float textEndAlpha,
    float duration)
    {
        if (duration <= 0f)
        {
            themeImage.color = imageEndColor;
            themeTitleText.alpha = textEndAlpha;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);

            // Makes the transition smoother.
            progress = Mathf.SmoothStep(0f, 1f, progress);

            themeImage.color = Color.Lerp(
                imageStartColor,
                imageEndColor,
                progress
            );

            themeTitleText.alpha = Mathf.Lerp(
                textStartAlpha,
                textEndAlpha,
                progress
            );

            yield return null;
        }

        themeImage.color = imageEndColor;
        themeTitleText.alpha = textEndAlpha;
    }

    private void HideImmediately()
    {
        if (introductionPanel != null)
            introductionPanel.SetActive(false);
    }

    private void ResumeGame()
    {
        if (!pausedByIntroduction)
            return;

        Time.timeScale = previousTimeScale;
        pausedByIntroduction = false;
    }

    private void OnDestroy()
    {
        // Prevent the game remaining paused if the scene changes mid-introduction.
        ResumeGame();
    }
}
