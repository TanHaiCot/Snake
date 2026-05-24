using System.Collections.Generic;
using UnityEngine;

public class SkillTreeManager : MonoBehaviour
{

    [Header("Database")]
    [SerializeField] private SkillTreeData database;
    [SerializeField] private SkillTreeUI skillTreeUI;

    private void Start()
    {
        skillTreeUI.BuildTree(this, database.AllSkills);
    }

    public bool IsLearned(SkillData skill)
    {
        return PlayerProgress.Instance.chosenSkillIds.Contains(skill.skillId);
    }

    public bool CanLearn(SkillData skill)
    {
        if (PlayerProgress.Instance.upgradePoints <= 0)
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
        if (skill.oppositeChoice == null)
            return false;

        return IsLearned(skill.oppositeChoice);
    }

    public void TryLearnSkill(SkillData skill)
    {
        if (!CanLearn(skill))
            return;

        PlayerProgress.Instance.chosenSkillIds.Add(skill.skillId);
        PlayerProgress.Instance.upgradePoints--;

        skillTreeUI.RefreshAllNodes();
    }
}
