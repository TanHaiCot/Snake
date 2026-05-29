using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillDescriptionPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text skillNameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Button learnButton;
    [SerializeField] private TMP_Text learnButtonText;

    private SkillData currentSkill;
    private SkillTreeUI ui;
    private SkillTreeManager manager;

    public void Show(SkillData skill, SkillTreeUI newUI, SkillTreeManager newManager)
    {
        currentSkill = skill;
        ui = newUI;
        manager = newManager;

        skillNameText.text = skill.skillName;
        descriptionText.text = skill.description;

        bool learned = manager.IsLearned(skill);
        bool canLearn = manager.CanLearn(skill);

        learnButton.interactable = canLearn;

        if (learned)
            learnButtonText.text = "LEARNED";
        else if (canLearn)
            learnButtonText.text = "LEARN";
        else
            learnButtonText.text = "LOCKED";

        learnButton.onClick.RemoveAllListeners();
        learnButton.onClick.AddListener(() => ui.LearnSkill(currentSkill));
    }
}