using UnityEngine;

public class SkillTreeManager : MonoBehaviour
{

    [Header("Database")]
    [SerializeField] private SkillTreeData database;
    [SerializeField] private SkillTreeUI skillTreeUI;

    [SerializeField] private GameObject continueButton;
    [SerializeField] private GameObject returnButton;

    private void Start()
    {
        PlayerProgress progress = PlayerProgress.EnsureInstance();
        progress.LoadSavedProgress();

        skillTreeUI.BuildTree(this, database.AllSkills);

        bool fromLevel = progress.openedSkillTreeFromLevel;

        if (continueButton != null)
            continueButton.SetActive(fromLevel);
    }

    public bool IsLearned(SkillData skill)
    {
        return PlayerProgress.EnsureInstance().chosenSkillIds.Contains(skill.skillId);
    }

    public bool CanLearn(SkillData skill)
    {
        if (PlayerProgress.EnsureInstance().upgradePoints <= 0)
            return false;

        if (IsLearned(skill))
            return false;

        if (IsOppositeAlreadyLearned(skill))
            return false;

        return RequirementsMet(skill);
    }

    public bool IsVisible(SkillData skill)
    {
        if (IsLearned(skill))
            return true;

        return RequirementsMet(skill);
    }

    private bool RequirementsMet(SkillData skill)
    {
        if (skill.requirementGroups == null || skill.requirementGroups.Length == 0)
            return true;

        foreach (SkillRequirementGroup group in skill.requirementGroups)
        {
            bool groupPassed = false;

            foreach (SkillData requiredSkill in group.oneOfTheseSkills)
            {
                if (requiredSkill != null && IsLearned(requiredSkill))
                {
                    groupPassed = true;
                    break;
                }
            }

            if (!groupPassed)
                return false;
        }

        return true;
    }

    private bool IsOppositeAlreadyLearned(SkillData skill)
    {
        if (skill.oppositeChoices == null || skill.oppositeChoices.Length == 0)
            return false;

        foreach (SkillData opposite in skill.oppositeChoices)
        {
            if (opposite != null && IsLearned(opposite))
            {
                Debug.Log(skill.skillName + " blocked by learned opposite: " + opposite.skillName);
                return true;
            }
        }

        return false; 
    }

    public void TryLearnSkill(SkillData skill)
    {
        if (!CanLearn(skill))
            return;

        PlayerProgress progress = PlayerProgress.EnsureInstance();
        progress.chosenSkillIds.Add(skill.skillId);
        progress.upgradePoints--;
        progress.SaveProgress();

        skillTreeUI.RefreshAllNodes();
    }

    public SkillNodeVisualState GetVisualState(SkillData skill)
    {
        if (IsLearned(skill))
            return SkillNodeVisualState.Learned;

        if (IsOppositeAlreadyLearned(skill))
        {
            //Debug.Log(skill.skillName + " is blocked because opposite is learned.");
            return SkillNodeVisualState.Blocked;
        }

        if (CanReveal(skill))
        {
            if (CanLearn(skill))
                return SkillNodeVisualState.Available;

           // Debug.Log(skill.skillName + " revealed but cannot learn. Points: "
           //+ PlayerProgress.EnsureInstance().upgradePoints);
            return SkillNodeVisualState.Blocked;
        }
        //Debug.Log(skill.skillName + " is blank because requirements are not met.");
        return SkillNodeVisualState.Blank;
    }

    public bool CanReveal(SkillData skill)
    {
        return RequirementsMet(skill);
    }

    public void ContinueToNextLevel()
    {
        PlayerProgress.EnsureInstance().openedSkillTreeFromLevel = false;
        SceneManagement.Instance.NextLevel();
    }

    public void ReturnToMenu()
    {
        PlayerProgress.EnsureInstance().openedSkillTreeFromLevel = false;

        SceneManagement.Instance.LoadScene("StartMenu");
    }

}
