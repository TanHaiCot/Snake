using UnityEngine;
using TMPro;
using UnityEngine.Events;
using System;

public class ScoreManager : MonoBehaviour
{
    [SerializeField] int targetScore;
    [SerializeField] TextMeshProUGUI scoreText;

    public UnityEvent<int, int> OnScoreChanged;   
    public UnityEvent OnTargetReached;

    int currentScore;

    private void Start()
    {
        UpdateUI();
    }

    public void SetTargetScore(int target)
    {
        targetScore = target;
        UpdateUI();
    }

    public void AddScore(int scoreAmount)
    {
        currentScore += scoreAmount; 
        if(currentScore > targetScore)
        {
            currentScore = targetScore;
        }
        UpdateUI();

        OnScoreChanged?.Invoke(currentScore, targetScore);   

        if (TargetReached)
        {
            OnTargetReached?.Invoke(); 
        }

    }

    private void UpdateUI()
    {
        if(scoreText != null)
        {
            scoreText.text = $"{currentScore}/{targetScore}"; 
        }
    }

    public void ResetScore()
    {
        currentScore = 0;
        UpdateUI();
    }

    // expression-bodied properties: 
    // Eg: TargetReached is true if currentScore >= targetScore 
    public bool TargetReached => currentScore >= targetScore;
}
