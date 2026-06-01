using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillNodeButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text skillText;
    private Image buttonImage;

    [Header("Colors")]
    [SerializeField] private Color blankColor = new Color(0.25f, 0.25f, 0.25f);
    [SerializeField] private Color availableColor = new Color(0.9f, 0.75f, 0.35f);
    [SerializeField] private Color learnedColor = new Color(1f, 1f, 0.7f);
    [SerializeField] private Color blockedColor = new Color(0.15f, 0.15f, 0.15f);

    private SkillData skill;
    private SkillTreeUI ui;
    private SkillTreeManager manager;

    public void Setup(SkillData newSkill, SkillTreeUI newUI, SkillTreeManager newManager)
    {
        skill = newSkill;
        ui = newUI;
        manager = newManager;

        //iconImage.sprite = skill.icon;
        buttonImage = GetComponent<Image>();

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => ui.SelectSkill(skill));

        Refresh();
    }

    public void Refresh()
    {
        SkillNodeVisualState state = manager.GetVisualState(skill);

        switch (state)
        {
            case SkillNodeVisualState.Blank:
                skillText.text = "";
                buttonImage.color = blankColor;
                button.interactable = false;
                break;

            case SkillNodeVisualState.Available:
                skillText.text = skill.skillName;
                buttonImage.color = availableColor;
                button.interactable = true;
                break;

            case SkillNodeVisualState.Learned:
                skillText.text = skill.skillName;
                buttonImage.color = learnedColor;
                button.interactable = true;
                break;

            case SkillNodeVisualState.Blocked:
                skillText.text = skill.skillName;
                buttonImage.color = blockedColor;
                button.interactable = true;
                break;
        }
    }
}
