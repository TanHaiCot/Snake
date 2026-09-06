using UnityEngine;

public class SkillOption 
{
    public SkillColumnData column;
    public SkillOptionBaseInfo baseInfo;
    public int optionIndex;

    public SkillData skill;
    public int offeredRank;
    public Vector2 uiPosition; 

    public SkillRank RankData =>
        skill != null ? skill.GetRank(offeredRank) : null;
   
    public string DisplayName
    {
        get
        {
            if (RankData != null && !string.IsNullOrWhiteSpace(RankData.skillNameWithRank))
            {
                return RankData.skillNameWithRank; 
            }

            return skill != null ? skill.skillName : "Unknown Skill";
        }
    }

    public string Description => RankData != null ? RankData.description : "No description available.";

}
