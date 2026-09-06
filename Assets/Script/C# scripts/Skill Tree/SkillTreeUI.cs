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
        public SkillColumnData column;
        public int optionIndex;
        public SkillNodeButton node; 
    }

    private readonly List<SkillSlot> slots = new();

    public void BuildTree(SkillTreeManager newManager, SkillColumnData[] columns)
    {
        manager = newManager;
        slots.Clear();

        foreach (SkillColumnData column in columns)
        {
            if(column == null || column.options == null)
                continue;

            for(int i = 0; i < column.options.Length; i++)
            {
                SkillNodeButton node = Instantiate(nodePrefab, nodeParent);
                node.GetComponent<RectTransform>().anchoredPosition = column.options[i].uiPosition;

                slots.Add(new SkillSlot
                {
                    column = column,
                    optionIndex = i,
                    node = node
                });
            }
        }

        RefreshAllNodes();
    }

    public void SelectSkill(SkillOption option)
    {
        if (option == null)
            return; 

        if (manager.GetVisualState(option) == SkillNodeVisualState.Blank)
            return;

        descriptionPanel.Show(option, this, manager);
    }

    public void LearnSkill(SkillOption option)
    {
        if(option == null)
            return;

        manager.TrySelect(option.column, option.optionIndex);
    }

    public void RefreshAllNodes()
    {
        foreach (SkillSlot slot in slots)
        {
            SkillOption option = manager.DetermineOption(slot.column, slot.optionIndex);
            slot.node.Setup(option, this, manager);
        }
    }
}
