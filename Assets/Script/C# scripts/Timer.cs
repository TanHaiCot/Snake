using System;
using TMPro;
using UnityEngine;

public class Timer : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI timerText;

    [SerializeField] float levelTimer;

    private float remainingTimer; 
    private float elapsedTimer;

    public event Action OnTimeUp;

    bool isFired; //check to make sure the code only run once  

    private void Awake()
    {
        ResetTimer(); 
    }

    public void SetLevelTimer(float timeLimit)
    {
        levelTimer = timeLimit;
        ResetTimer();
    }

    private void Update()
    {
        if (remainingTimer > 0)
        {
            elapsedTimer += Time.deltaTime;
            remainingTimer = Mathf.Max(remainingTimer - Time.deltaTime, 0f);
  
            if (remainingTimer == 0 && !isFired)
            {
                isFired = true;
                OnTimeUp?.Invoke();
            }
        }

        UpdateUI(); 
    }

    void UpdateUI()
    {
        int minute = Mathf.FloorToInt(remainingTimer / 60);
        int second = Mathf.FloorToInt(remainingTimer % 60);
        if (timerText)
            timerText.text = string.Format("{0:00}:{1:00}", minute, second);  //0:00 mean 1st argument, 2 digits (0 is the priority of the argument, : is the format specifier, 00 means show two digits
                                                                              //1:00 is the same but 1 is for 2nd argument 
    }

    public void ResetTimer()
    {
        remainingTimer = levelTimer; 
        elapsedTimer = 0f;
        isFired = false;
        UpdateUI(); 
    }

    public void AddTime(float amount)
    {
        if (amount <= 0f)
            return;

        remainingTimer += amount;
    }

    public bool TimeUp => remainingTimer <= 0f;

    public float RemainingTime => remainingTimer;

    public float ElapsedTime => elapsedTimer;

    public float LevelTime => levelTimer;
}
