using System.Collections.Generic;
using UnityEngine;

public class SkillTreeUI : MonoBehaviour
{
    [SerializeField] private RectTransform nodeParent;
    [SerializeField] private SkillNodeButton nodePrefab;
    [SerializeField] private SkillDescriptionPanel descriptionPanel;

    private SkillTreeManager manager;

    private class SkillSlot
    {
        public Vector2 position;
        public List<SkillData> skills = new();
        public SkillNodeButton node;
        public SkillData currentSkill;
    }

    private List<SkillSlot> slots = new();

    public void BuildTree(SkillTreeManager newManager, SkillData[] skills)
    {
        manager = newManager;
        slots.Clear();

        foreach (SkillData skill in skills)
        {
            SkillSlot slot = FindSlot(skill.uiPosition);

            if (slot == null)
            {
                slot = new SkillSlot();
                slot.position = skill.uiPosition;
                slots.Add(slot);

                SkillNodeButton node = Instantiate(nodePrefab, nodeParent);
                node.GetComponent<RectTransform>().anchoredPosition = skill.uiPosition;
                slot.node = node;
            }

            slot.skills.Add(skill);
        }

        RefreshAllNodes();
    }

    private SkillSlot FindSlot(Vector2 position)
    {
        foreach (SkillSlot slot in slots)
        {
            if (slot.position == position)
                return slot;
        }

        return null;
    }

    private SkillData GetBestSkillForSlot(SkillSlot slot)
    {
        SkillData bestSkill = slot.skills[0];
        int bestPriority = -1;

        foreach (SkillData skill in slot.skills)
        {
            SkillNodeVisualState state = manager.GetVisualState(skill);
            int priority = GetStatePriority(state);

            if (priority > bestPriority)
            {
                bestPriority = priority;
                bestSkill = skill;
            }
        }

        return bestSkill;
    }

    private int GetStatePriority(SkillNodeVisualState state)
    {
        switch (state)
        {
            case SkillNodeVisualState.Learned:
                return 4;

            case SkillNodeVisualState.Available:
                return 3;

            case SkillNodeVisualState.Blocked:
                return 2;

            case SkillNodeVisualState.Blank:
            default:
                return 1;
        }
    }

    public void SelectSkill(SkillData skill)
    {
        if (manager.GetVisualState(skill) == SkillNodeVisualState.Blank)
            return;

        descriptionPanel.Show(skill, this, manager);
    }

    public void LearnSkill(SkillData skill)
    {
        manager.TryLearnSkill(skill);
        RefreshAllNodes();
    }

    public void RefreshAllNodes()
    {
        foreach (SkillSlot slot in slots)
        {
            SkillData skillToShow = GetBestSkillForSlot(slot);
            slot.currentSkill = skillToShow;
            slot.node.Setup(skillToShow, this, manager);
        }
    }
}
