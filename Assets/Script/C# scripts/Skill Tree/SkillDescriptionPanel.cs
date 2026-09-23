using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillDescriptionPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text skillNameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Button learnButton;
    [SerializeField] private TMP_Text learnButtonText;

    private SkillOption currentSkill;
    private SkillTreeUI ui;
    private SkillTreeManager manager;

    public void Show(SkillOption skill, SkillTreeUI newUI, SkillTreeManager newManager)
    {
        gameObject.SetActive(true);

        currentSkill = skill;
        ui = newUI;
        manager = newManager;

        skillNameText.text = skill.DisplayName;
        descriptionText.text = skill.Description;

        SkillNodeVisualState state = manager.GetVisualState(skill);

        bool learned = state == SkillNodeVisualState.Learned;

        bool canLearn = manager.CanLearn(skill);

        learnButton.interactable = canLearn;

        if (learned)
            learnButtonText.text = "LEARNED";
        else if (canLearn)
            learnButtonText.text = "LEARN";
        else
            learnButtonText.text = "LOCKED";

        learnButton.onClick.RemoveAllListeners();
        learnButton.onClick.AddListener(HandleLearnButtonClick);

    }

    public void Hide()
    {
        currentSkill = null; 
        learnButton.interactable = false;
        gameObject.SetActive(false);
    }

    private void HandleLearnButtonClick()
    {
        if (currentSkill == null || ui == null)
            return;

        if (ui.LearnSkill(currentSkill))
        {
            AudioManager audio = AudioManager.Instance;

            if (audio != null)
                audio.playSFX(audio.updateSkill);
        }
    }
}