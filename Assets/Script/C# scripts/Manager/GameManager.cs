using NUnit.Framework.Interfaces;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;


public class GameManager : MonoBehaviour
{
    [Header("MANAGERS")]
    [SerializeField] UIManager uiManager;
    [SerializeField] ScoreManager scoreManager;
    [SerializeField] MapManager mapManager;
    [SerializeField] DoorManager doorManager;

    [Header("Game Components")]
    [SerializeField] Snake snake;
    //[SerializeField] Snake secondSnake; 
    [SerializeField] Timer timer;
    [SerializeField] Food food;
    [SerializeField] AI_Snake ai_Snake;
    [SerializeField] Energy energy;
    [SerializeField] SnakeAbilities snakeAbilities;
    [SerializeField] LevelSummaryUI levelSummaryUI;
    [SerializeField] DialogueData reverseMovementDialogue;
    [SerializeField] DialogueData skillTreeUnlockedDialogue; 

    [Header("Game State")]
    private bool isLost; 
    private bool isExitOpen;
    private bool isPaused;

    [Header("Game Opening")]
    [SerializeField] GameObject blackScreen;
    [SerializeField] DialogueData openingDialogue;


    [SerializeField] LevelData LevelData;
    private float readyDelay = 0.75f;

    private SkillRuntimeApplier skillRuntimeApplier;

    void Start() 
    {
        if (scoreManager != null)
        {
            scoreManager.ResetScore();
            if(LevelData != null)
                scoreManager.SetTargetScore(LevelData.targetScore);
        }

        if(timer != null && LevelData != null)
        {
            if(LevelData.isShowingLevelSummary)
                timer.StartStopwatch();

            else 
                timer.StartCountdown(LevelData.timeLimit);
        }

        if(LevelData != null)
        {
            AudioManager.Instance?.PlayMusic(LevelData.backgroundMusic);    
        }

        ResetAndApplySkillRuntime(true);
        
        mapManager.InitAndBuildMap();

        if(food.FoodSpawnedAfterEat)
            food.RandomizedSpawn();

        StartCoroutine(BeginLevelSequence());
        
    }

    private IEnumerator BeginLevelSequence()
    {
        snake.SetInputEnabled(false);
        Time.timeScale = 0f;

        // Wait one rendered frame so all instantiated body sprites appear.
        yield return null;

        // Wait in real time because the game is paused.
        yield return new WaitForSecondsRealtime(readyDelay);
       
        if (LevelData.isShowingOpeningDialogue)
        {
            StartOpeningDialogue(); 
        }
        

        else if (LevelData != null && LevelData.enableReverseMovement)
        {
            StartReverseMovementDialogue();
        }
        else
        {
            snake.SetInputEnabled(true);
            Time.timeScale = 1f;
        }
    }

    public void Update()    
    {
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            if (isPaused == false)
            {
                Pause();
            }
            else
            {
                Resume();
            }
        }
    }

    private void OnEnable()
    {
        if (timer)
            timer.OnTimeUp += HandleTimeUp; 
        
        if(snake != null)
        {
            snake.OnFoodEatenByPlayer.AddListener(HandleFoodEaten);
            snake.OnTeleGateTrigger.AddListener(HandleTeleGateTrigger);
        }
     
        if(ai_Snake != null)
        {
            ai_Snake.OnFoodEatenByPlayer.AddListener(HandleFoodEaten);  
            ai_Snake.OnTeleGateTrigger.AddListener(HandleTeleGateTrigger);
        }   
    }


    //private void HandleScoreChanged(int current, int target)
    //{
    //    if (!LevelData.enableReverseMovement)
    //        return;

    //    if (!reverseMovementStarted && current >= scoreToStartReverseMovement)
    //    {
    //        reverseMovementStarted = true;
    //        StartReverseMovementDialogue();
    //        //snake.SetReverseMovement(true);
    //    }
    //}

    private void StartOpeningDialogue()
    {
        if (openingDialogue == null || DialogueManager.Instance == null)
            return;
      
        //blackScreen.SetActive(true);

        DialogueManager.Instance.StartDialogue(openingDialogue, () =>
        {
            StartCoroutine(FinishOpeningDialogue());
        });
    }

    private IEnumerator FinishOpeningDialogue()
    {
        blackScreen.SetActive(false);
        
        yield return new WaitForSecondsRealtime(readyDelay);

        snake.SetInputEnabled(true);
        Time.timeScale = 1f;
    }

    private void StartReverseMovementDialogue()
    {
        Time.timeScale = 0f;
        snake.SetInputEnabled(false);

        DialogueManager.Instance.StartDialogue(reverseMovementDialogue, () =>
        {
            StartCoroutine(FinishReverseMovementDialogue());
        });
    }

    private IEnumerator FinishReverseMovementDialogue()
    {
        yield return new WaitForSecondsRealtime(readyDelay);

        snake.SetReverseMovement(true);
        snake.SetInputEnabled(true);
        Time.timeScale = 1f;
    }

    private void HandleFoodEaten(bool isEatenByPlayer)
    {
        if (isEatenByPlayer)
        {
            AudioManager.Instance?.EatingSound(true);
            EnsureSkillRuntime();
            skillRuntimeApplier.ApplyFoodEatenEffects();
        }
        else 
        {
            AudioManager.Instance?.EatingSound(false);
        }
    }
    private void HandleTeleGateTrigger()
    {
        AudioManager.Instance?.playSFX(AudioManager.Instance.teleport);
    }

    private void OnDisable()
    {
        if (timer)
            timer.OnTimeUp -= HandleTimeUp; 
        
        if(snake != null)
        {
            snake.OnFoodEatenByPlayer.RemoveListener(HandleFoodEaten);
            snake.OnTeleGateTrigger.RemoveListener(HandleTeleGateTrigger);
        }

        if(ai_Snake != null)
        {
            ai_Snake.OnFoodEatenByPlayer.RemoveListener(HandleFoodEaten);
            ai_Snake.OnTeleGateTrigger.RemoveListener(HandleTeleGateTrigger);
        }
    }

    private void HandleTimeUp()
    {
        if (!scoreManager.TargetReached)
        {
            GameOver(); 
        }
    }

    public void CheckWinStatus()
    {
        if (isExitOpen || isLost)
            return;

        if(scoreManager.TargetReached/* && !timer.TimeUp*/)
        {
            if(LevelData != null && LevelData.isShowingLevelSummary)
            {
                Debug.Log("Level Summary is showing");
                ShowLevelSummary();
            }
            else if(LevelData != null && !LevelData.isShowingLevelSummary && !timer.TimeUp)
            {
                OpenLevelExit();
            }
        }
    }

    private void OpenLevelExit()
    {
        isExitOpen = true;

        bool isSkiilTreeUnlockedForTheFirstTime = LevelData != null && LevelData.isSkillTreeUnlocked && !PlayerProgress.EnsureInstance().skillTreeUnlocked;

        AudioManager.Instance?.playSFX(AudioManager.Instance.doorOpened);
        doorManager.Open();

        if(isSkiilTreeUnlockedForTheFirstTime)
        {
            SkillTreeUnlockedDialogue(); 
        }
    }

    private void SkillTreeUnlockedDialogue()
    {
        if(skillTreeUnlockedDialogue == null || DialogueManager.Instance == null)
            return;
 
        Time.timeScale = 0f;
        snake.SetInputEnabled(false);

        DialogueManager.Instance.StartDialogue(skillTreeUnlockedDialogue, () =>
        {
            snake.SetInputEnabled(true);
            Time.timeScale = 1f;
        });
    }

    public void MoveToSkillTree()
    {
        if (energy != null)
            energy.StoreCurrentEnergy();

        bool levelUnlocksSkillTree = LevelData != null && LevelData.isSkillTreeUnlocked;
        PlayerProgress.EnsureInstance().CompleteLevel(SceneManager.GetActiveScene().buildIndex, levelUnlocksSkillTree);

        //if(levelUnlocksSkillTree == false)
        //{
        //    Time.timeScale = 1f;
        //    SceneManagement.EnsureInstance().NextLevel();
        //    return; 
        //}

        Time.timeScale = 1f; 
        PlayerProgress.EnsureInstance().openedSkillTreeFromLevel = true;
        SceneManagement.EnsureInstance().LoadScene("SkillTree");
    }

    public void ShowLevelSummary()
    {
        if (levelSummaryUI != null && LevelData.isShowingLevelSummary && timer != null && levelSummaryUI != null)
        {
            timer.StopTimer(); 
            snake.SetInputEnabled(false);

            float resultTime = timer.ElapsedTime;
            string grade = CalculateGrade(resultTime);

            levelSummaryUI.ShowSummary(resultTime, grade);

            Time.timeScale = 0f;
            return; 
        }
    }

    public void ContinueToNextLevelAfterSummary()
    {
        //if (!summaryIsShowing)
        //    return;

        PlayerProgress.EnsureInstance().CompleteLevel(SceneManager.GetActiveScene().buildIndex, false);

        Time.timeScale = 1f;
        SceneManagement.EnsureInstance().NextLevel();
        return;
    }

    private string CalculateGrade(float elapsedTime)
    {
        if (elapsedTime <= LevelData.gradeSTime)
            return "S";

        if (elapsedTime <= LevelData.gradeATime)
            return "A";

        if (elapsedTime <= LevelData.gradeBTime)
            return "B";

        if (elapsedTime <= LevelData.gradeCTime)
            return "C";

        return "D";
    }

    public void Resume()
    {
        uiManager.HidePauseMenu();
        Time.timeScale = 1f;
        isPaused = false;
    }

    public void Pause()
    {
        uiManager.ShowPauseMenu();
        Time.timeScale = 0f;
        isPaused = true;
    }

    public void Restart()
    {
        Time.timeScale = 1f;

        mapManager.InitAndBuildMap(); 

        snake.Restate();

        if (food.FoodSpawnedAfterEat)
            food.RandomizedSpawn();

        //if (secondSnake != null)
        //    secondSnake.Restate();

        if (ai_Snake != null)
            ai_Snake.Restate();

        isPaused = false;
        isLost = false; 
        isExitOpen = false;

        uiManager.HideLostMenu();

        scoreManager.ResetScore();
        scoreManager.SetTargetScore(LevelData.targetScore);

        if (LevelData.isShowingLevelSummary)
            timer.StartStopwatch();

        else
            timer.StartCountdown(LevelData.timeLimit);

        ResetAndApplySkillRuntime(false);

        snake.SetReverseMovement(false);

        if (LevelData != null && LevelData.enableReverseMovement)
        {
            StartReverseMovementDialogue();
        }
    }

    public void MainMenu()
    {
        if (energy != null)
        {
            energy.StoreCurrentEnergy();
            PlayerProgress.EnsureInstance().SaveProgress();
        }

        Time.timeScale = 1f; 
        SceneManager.LoadScene("StartMenu");
    }

    public void GameOver()
    {
        Time.timeScale = 0f;
        isLost = true;
        if (isLost == true)
        {
            uiManager.ShowLostMenu();
        }
    }

    private void ResetAndApplySkillRuntime(bool restoreSavedEnergy)
    {
        if (energy != null)
        {
            energy.ResetSkillAdjustedStats();
            energy.ResetEnergy();
        }
        
        if (snakeAbilities != null)
            snakeAbilities.ResetAbilities(CanPlayerNormallyUseAbilities());

        skillRuntimeApplier = new SkillRuntimeApplier(snakeAbilities, energy, timer);
        skillRuntimeApplier.ApplyLearnedSkills();

        if (energy != null)
        {
            if (restoreSavedEnergy)
                energy.RestoreCurrentEnergy();
            else
                energy.StoreCurrentEnergy();
        }
    }

    private void EnsureSkillRuntime()
    {
        if (skillRuntimeApplier != null)
            return;

        skillRuntimeApplier = new SkillRuntimeApplier(snakeAbilities, energy, timer);
        skillRuntimeApplier.ApplyLearnedSkills();
    }

    private bool CanPlayerNormallyUseAbilities()
    {
        return SceneManager.GetActiveScene().name != "Boss1Fight";
    }
}
