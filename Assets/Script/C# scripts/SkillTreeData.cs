using UnityEngine;

[CreateAssetMenu(fileName = "SkillTreeData", menuName = "Scriptable Objects/SkillTreeData")]
public class SkillTreeData : ScriptableObject
{
    [SerializeField] private SkillData[] allSkills;

    public SkillData[] AllSkills => allSkills;

    public SkillData GetSkillById(string id)
    {
        foreach (SkillData skill in allSkills)
        {
            if (skill.skillId == id)
                return skill;
        }

        return null;
    }
}
