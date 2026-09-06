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

        skillTreeUI.BuildTree(this, database.AllColumns);

        bool fromLevel = progress.openedSkillTreeFromLevel;

        if (continueButton != null)
            continueButton.SetActive(fromLevel);
    }

    public bool IsColumnComplete(SkillColumnData column)
    {
        return column != null && PlayerProgress.EnsureInstance().HasColumnSelection(column.columnId);
    }

    public bool ArePrerequisitesMet(SkillColumnData column)
    {
        if (column == null)
            return false;

        if (column.prerequisiteColumns == null || column.prerequisiteColumns.Length == 0)
            return true; 

        foreach(SkillColumnData prerequisite in column.prerequisiteColumns)
        {
            if(!IsColumnComplete(prerequisite))
            {
                return false;
            }   
        }

        return true; 
    }

    public SkillOption DetermineOption(SkillColumnData column, int optionIndex)
    {
        if(column == null || column.options == null || optionIndex < 0 || optionIndex >= column.options.Length)
            return null;

        PlayerProgress progress = PlayerProgress.EnsureInstance();

        ColumnSelectionProgress selected = progress.GetColumnSelection(column.columnId);

        if (selected != null)
        {
            string savedRootId = optionIndex == 0 ? selected.firstGivenRootId : selected.secondGivenRootId;
            int savedRank = optionIndex == 0 ? selected.firstGivenRank : selected.secondGivenRank;

            SkillData savedSkill = database.GetSkillByRootId(savedRootId);

            if (savedSkill != null && savedRank > 0)
            {
                return new SkillOption
                {
                    column = column,
                    baseInfo = column.options[optionIndex],
                    optionIndex = optionIndex,
                    skill = savedSkill,
                    offeredRank = savedRank,
                    uiPosition = column.options[optionIndex].uiPosition
                };
            }
        }

        SkillOptionBaseInfo baseInfo = column.options[optionIndex];
        SkillData skill = DetermineSkillFromBaseInfo(baseInfo);

        if(skill == null)
            return null;

        int currentRank = progress.GetSkillRank(skill.rootId);

        int offeredRank = baseInfo.optionType == SkillOptionType.NewSkill ? 1 : currentRank + 1;

        if(baseInfo.optionType == SkillOptionType.NewSkill && currentRank > 0)
            return null;

        if (offeredRank < 1 || offeredRank > skill.MaxRank)
        {
            return null;
        }

        return new SkillOption
        {
            column = column,
            baseInfo = baseInfo,
            optionIndex = optionIndex,
            skill = skill,
            offeredRank = offeredRank,
            uiPosition = baseInfo.uiPosition
        };
    }

    private SkillData DetermineSkillFromBaseInfo(SkillOptionBaseInfo baseInfo)
    {
        if (baseInfo == null)
            return null;

        switch (baseInfo.optionType)
        {
            case SkillOptionType.NewSkill:
                return baseInfo.newSkill;

            case SkillOptionType.SkillUpgradeChosen:
                return GetChosenSkill(baseInfo.sourceColumn);

            case SkillOptionType.SkillUpgradeSkipped:
                return GetSkippedSkill(baseInfo.sourceColumn);

            default:
                return null;
        }
    }

    private SkillData GetChosenSkill(SkillColumnData sourceColumn)
    {
        if (sourceColumn == null)
            return null;

        PlayerProgress progress =
            PlayerProgress.EnsureInstance();

        ColumnSelectionProgress selection =
            progress.GetColumnSelection(sourceColumn.columnId);

        if (selection == null)
            return null;

        return database.GetSkillByRootId(
            selection.chosenRootId);
    }

    private SkillData GetSkippedSkill(SkillColumnData sourceColumn)
    {
        if (sourceColumn == null)
            return null;

        PlayerProgress progress = PlayerProgress.EnsureInstance();

        ColumnSelectionProgress selection = progress.GetColumnSelection(sourceColumn.columnId);

        if (selection == null)
            return null;

        string skippedRootId = selection.GetUnchosenRootId(); 
        
        if(!progress.HasSkillRoot(skippedRootId))
            return null;

        return database.GetSkillByRootId(skippedRootId);
    }

    public bool CanSelect(SkillOption option)
    {
        if (option == null ||
            option.column == null ||
            option.skill == null)
        {
            return false;
        }

        PlayerProgress progress =
            PlayerProgress.EnsureInstance();

        if (progress.upgradePoints <= 0)
            return false;

        if (progress.HasColumnSelection(
            option.column.columnId))
        {
            return false;
        }

        if (!ArePrerequisitesMet(option.column))
            return false;

        int currentRank =
            progress.GetSkillRank(option.skill.rootId);

        if (option.baseInfo.optionType == SkillOptionType.NewSkill)
        {
            return currentRank == 0 &&
                   option.offeredRank == 1;
        }

        return currentRank > 0 &&
               option.offeredRank == currentRank + 1 &&
               progress.CanUpgradeSkill(option.skill);
    }

    public bool TrySelect(
    SkillColumnData column,
    int optionIndex)
    {
        SkillOption selected =
            DetermineOption(column, optionIndex);

        if (!CanSelect(selected))
            return false;

        SkillOption first =
            DetermineOption(column, 0);

        SkillOption second =
            DetermineOption(column, 1);

        PlayerProgress progress =
            PlayerProgress.EnsureInstance();

        if (!progress.UpgradeSkill(selected.skill))
            return false;

        progress.RecordColumnSelection(
            column.columnId,

            first?.skill.rootId,
            first?.offeredRank ?? 0,

            second?.skill.rootId,
            second?.offeredRank ?? 0,

            optionIndex,
            selected.skill.rootId,
            selected.offeredRank);

        progress.upgradePoints--;
        progress.SaveProgress();

        skillTreeUI.RefreshAllNodes();
        return true;
    }

    public SkillNodeVisualState GetVisualState(SkillOption option)
    {
        if (option == null)
            return SkillNodeVisualState.Blank;

        PlayerProgress progress =
            PlayerProgress.EnsureInstance();

        ColumnSelectionProgress selection =
            progress.GetColumnSelection(
                option.column.columnId);

        if (selection != null)
        {
            return selection.chosenOptionIndex ==
                   option.optionIndex
                ? SkillNodeVisualState.Learned
                : SkillNodeVisualState.Blocked;
        }

        if (!ArePrerequisitesMet(option.column))
            return SkillNodeVisualState.Blank;

        return CanSelect(option)
            ? SkillNodeVisualState.Available
            : SkillNodeVisualState.Blocked;
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
