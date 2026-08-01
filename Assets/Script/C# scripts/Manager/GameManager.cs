using System;
using UnityEngine;
using UnityEngine.SceneManagement;


public class GameManager : MonoBehaviour
{
    [Header("MANAGERS")]
    [SerializeField] UIManager uiManager;
    [SerializeField] ScoreManager scoreManager;
    [SerializeField] MapManager mapManager;
    [SerializeField] DoorController doorController;

    [Header("Game Components")]
    [SerializeField] Snake snake;
    //[SerializeField] Snake secondSnake; 
    [SerializeField] Timer timer;
    [SerializeField] Food food;
    [SerializeField] AI_Snake ai_Snake;
    [SerializeField] Energy energy;
    [SerializeField] SnakeAbilities snakeAbilities;
    [SerializeField] Dialogue reverseMovementDialogue;

    [Header("Game State")]
    private bool isLost; 
    private bool isExitOpen;
    private bool isPaused;

    [SerializeField] LevelData LevelData;

    private int scoreToStartReverseMovement = 3;
    private bool reverseMovementStarted;
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
            timer.SetLevelTimer(LevelData.timeLimit);
        }

        ResetAndApplySkillRuntime(true);
        
        mapManager.InitAndBuildMap();
        food.RandomizedSpawn(); 
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

        if (scoreManager != null)
            scoreManager.OnScoreChanged.AddListener(HandleScoreChanged);
    }


    private void HandleScoreChanged(int current, int target)
    {
        if (!LevelData.enableReverseMovement)
            return;

        if (!reverseMovementStarted && current >= scoreToStartReverseMovement)
        {
            reverseMovementStarted = true;
            StartReverseMovementDialogue();
            //snake.SetReverseMovement(true);
        }
    }

    private void StartReverseMovementDialogue()
    {
        Time.timeScale = 0f;
        snake.SetInputEnabled(false);

        DialogueManager.Instance.StartDialogue(reverseMovementDialogue, () =>
        {
            snake.SetReverseMovement(true);
            snake.SetInputEnabled(true);
            Time.timeScale = 1f;
        });
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

        if (scoreManager != null)
            scoreManager.OnScoreChanged.RemoveListener(HandleScoreChanged);
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

        if(scoreManager.TargetReached && !timer.TimeUp)
        {
            OpenLevelExit();
        }
    }

    private void OpenLevelExit()
    {
        isExitOpen = true;
        //AudioManager.Instance?.playSFX(AudioManager.Instance.doorOpened);
        doorController.Open();
    }

    public void MoveToSkillTree()
    {
        if (energy != null)
            energy.StoreCurrentEnergy();

        bool levelUnlocksSkillTree = LevelData != null && LevelData.isSkillTreeUnlocked;
        PlayerProgress.EnsureInstance().CompleteLevel(SceneManager.GetActiveScene().buildIndex, levelUnlocksSkillTree);

        if(levelUnlocksSkillTree == false)
        {
            Time.timeScale = 1f;
            SceneManagement.EnsureInstance().NextLevel();
            return; 
        }

        Time.timeScale = 1f; 
        PlayerProgress.EnsureInstance().openedSkillTreeFromLevel = true;
        SceneManagement.EnsureInstance().LoadScene("SkillTree");
    }

    //public void MoveToNextLevel()
    //{
    //    Time.timeScale = 1f;
    //    SceneManagement.Instance.NextLevel();
    //}

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

        timer.SetLevelTimer(LevelData.timeLimit);

        ResetAndApplySkillRuntime(false);

        reverseMovementStarted = false;
        snake.SetReverseMovement(false);
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
