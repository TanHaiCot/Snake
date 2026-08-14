using TMPro;
using UnityEngine;
using static UnityEngine.InputSystem.LowLevel.InputStateHistory;

public class LevelSummaryUI : MonoBehaviour
{
    [SerializeField] GameObject summaryPanel;
    [SerializeField] TextMeshProUGUI timeText;
    [SerializeField] TextMeshProUGUI gradeText;

    public void ShowSummary(float elapsedTime, string grade)
    {
        int minutes = Mathf.FloorToInt(elapsedTime / 60);
        int seconds = Mathf.FloorToInt(elapsedTime % 60);
        int milliseconds =
            Mathf.FloorToInt((elapsedTime % 1f) * 100f);
            
        if (timeText != null)
            timeText.text = string.Format("{0:00}:{1:00}:{2:00}", minutes, seconds, milliseconds);
        
        if (gradeText != null)
            gradeText.text = string.Format("Grade: {0}", grade);

        if (summaryPanel != null)
            summaryPanel.SetActive(true);
    }

    public void HideSummary()
    {
        if (summaryPanel != null)
            summaryPanel.SetActive(false);
    }
}
