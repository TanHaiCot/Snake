using UnityEngine;

[System.Serializable]
public class SkillRank
{
    public string skillNameWithRank;

    [TextArea]
    public string description;

    public float value;
}

[CreateAssetMenu(fileName = "SkillData", menuName = "Scriptable Objects/SkillData")]
public class SkillData : ScriptableObject
{
    [Header("Skill Info")]
    public string rootId;
    public string skillName;

    [Header("Effect")]
    public SkillEffectType effectType;

    [Header("Ranks")]
    public SkillRank[] ranks;

    public int MaxRank
    {
        get
        {
            return ranks != null ? ranks.Length : 0;
        }
    }

    public SkillRank GetRank(int rank)
    {
        if (rank < 1 || rank > MaxRank)
            return null;

        return ranks[rank - 1];
    }
}

