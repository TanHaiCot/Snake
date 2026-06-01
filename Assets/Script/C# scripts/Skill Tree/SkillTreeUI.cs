using System.Collections.Generic;
using UnityEngine;

public class SkillTreeUI : MonoBehaviour
{
    [SerializeField] private RectTransform nodeParent;
    [SerializeField] private SkillNodeButton nodePrefab;
    [SerializeField] private SkillDescriptionPanel descriptionPanel;

    private SkillTreeManager manager;
    private SkillNodeButton[] spawnedNodes;

    public void BuildTree(SkillTreeManager newManager, SkillData[] skills)
    {
        manager = newManager;
        spawnedNodes = new SkillNodeButton[skills.Length];

        for (int i = 0; i < skills.Length; i++)
        {
            SkillNodeButton node = Instantiate(nodePrefab, nodeParent);
            node.GetComponent<RectTransform>().anchoredPosition = skills[i].uiPosition;
            node.Setup(skills[i], this, manager);
            spawnedNodes[i] = node;
        }

        RefreshAllNodes();
    }

    public void SelectSkill(SkillData skill)
    {
        descriptionPanel.Show(skill, this, manager);
    }

    public void LearnSkill(SkillData skill)
    {
        manager.TryLearnSkill(skill);
    }

    public void RefreshAllNodes()
    {
        foreach (SkillNodeButton node in spawnedNodes)
        {
            node.Refresh();
        }
    }
}
