using UnityEngine;

[CreateAssetMenu(fileName = "SkillTreeData", menuName = "Scriptable Objects/SkillTreeData")]
public class SkillTreeData : ScriptableObject
{
    [SerializeField] private SkillData[] allSkills;
    [SerializeField] private SkillColumnData[] allColumns;

    public SkillData[] AllSkills => allSkills;
    public SkillColumnData[] AllColumns => allColumns;

    public SkillData GetSkillByRootId(string rootId)
    {
        if(string.IsNullOrWhiteSpace(rootId) || allSkills == null)
            return null;

        foreach (SkillData skill in allSkills)
        {
            if (skill != null && skill.rootId == rootId)
                return skill;
        }

        return null;
    }
    public SkillColumnData GetColumnById(string columnId)
    {
        if (string.IsNullOrWhiteSpace(columnId) ||
            allColumns == null)
        {
            return null;
        }

        foreach (SkillColumnData column in allColumns)
        {
            if (column != null &&
                column.columnId == columnId)
            {
                return column;
            }
        }

        return null;
    }

}
