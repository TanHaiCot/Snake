using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillNodeButton : MonoBehaviour
{
    //[SerializeField] private Image iconImage;
    //[SerializeField] private Image lockImage;
    //[SerializeField] private Image learnedImage;
    [SerializeField] private Button button;
    //[SerializeField] private TMP_Text levelText;

    private SkillData skill;
    private SkillTreeUI ui;
    private SkillTreeManager manager;

    public void Setup(SkillData newSkill, SkillTreeUI newUI, SkillTreeManager newManager)
    {
        skill = newSkill;
        ui = newUI;
        manager = newManager;

        //iconImage.sprite = skill.icon;
        button.onClick.AddListener(() => ui.SelectSkill(skill));

        Refresh();
    }

    public void Refresh()
    {
        bool learned = manager.IsLearned(skill);
        bool visible = manager.IsVisible(skill);
        bool canLearn = manager.CanLearn(skill);

        gameObject.SetActive(visible);

        //lockImage.gameObject.SetActive(visible && !learned && !canLearn);
        //learnedImage.gameObject.SetActive(learned);

        button.interactable = visible;
    }
}
