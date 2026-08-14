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

    private bool timeIsUp;        //check to make sure the time up code only run once  
    private bool isRunning;
    private bool countUp; 


    private void Update()
    {
        if (!isRunning)
            return; 

        if(countUp)
        {
            elapsedTimer += Time.deltaTime;
        }
        else if (remainingTimer > 0)
        {
            elapsedTimer += Time.deltaTime;
            remainingTimer = Mathf.Max(remainingTimer - Time.deltaTime, 0f);
  
            if (remainingTimer == 0 && !timeIsUp)
            {
                timeIsUp = true;
                OnTimeUp?.Invoke();
            }
        }

        UpdateUI(); 
    }

    void UpdateUI()
    {
        float displayTime = countUp ? elapsedTimer : remainingTimer;

        int minute = Mathf.FloorToInt(displayTime / 60);
        int second = Mathf.FloorToInt(displayTime % 60);

        if (timerText)
            timerText.text = string.Format("{0:00}:{1:00}", minute, second);  //0:00 mean 1st argument, 2 digits (0 is the priority of the argument, : is the format specifier, 00 means show two digits
                                                                              //1:00 is the same but 1 is for 2nd argument 
    }

    public void StartStopwatch()
    {
        countUp = true;
        remainingTimer = 0;
        elapsedTimer = 0;
        timeIsUp = false;
        isRunning = true;
        UpdateUI(); 
    }

    public void StartCountdown(float timeLimit)
    {
        countUp = false;
        remainingTimer = levelTimer;
        levelTimer = timeLimit; 
        elapsedTimer = 0f;
        timeIsUp = false;
        isRunning = false; 
        UpdateUI(); 
    }

    public void StopTimer()
    {
        isRunning = false;
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
