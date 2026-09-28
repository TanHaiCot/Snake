using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System;
using System.Collections;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    // Remember which dialogues started in each level during this visit.
    private static readonly HashSet<(string level, DialogueData dialogue)>
        playedDialogues = new HashSet<(string, DialogueData)>();

    public static void ResetDialogueHistory()
    {
        playedDialogues.Clear();
    }

    // Also clear when starting a fresh Play session in the Editor.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        ResetDialogueHistory();
    }


    [Header("UI")]
    //public Image characterIcon;
    public TextMeshProUGUI characterName;
    public TextMeshProUGUI dialogueArea;
    [SerializeField] GameObject characterNamePanel;
    [SerializeField] GameObject dialoguePanel; 
    //public Animator animator;

    [Header("Typing")]
    public float typingSpeed = 0.03f;

    private Queue<DialogueLine> lines;
    private Action onDialogueFinished;

    public bool isDialogueActive = false;
    private bool isTyping = false;
    private string currentFullLine;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        lines = new Queue<DialogueLine>();

        if (characterNamePanel != null)
            characterNamePanel.SetActive(false);

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false); 
    }

    private void Update()
    {
        //if (!isDialogueActive)
        //    return;

        //if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        //{
        //    DisplayNextDialogueLine();
        //}
    }

    public void StartDialogue(DialogueData dialogue, Action onFinished = null)
    {
        if (dialogue == null)
        {
            onFinished?.Invoke();
            return;
        }

        string level = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;

        // Add returns false if this dialogue already started in this level.
        if (!playedDialogues.Add((level, dialogue)))
        {
            // Still run the normal completion logic:
            // enable movement, resume time, apply reverse movement, etc.
            onFinished?.Invoke();
            return;
        }

        isDialogueActive = true;
        onDialogueFinished = onFinished;

        //animator.Play("show");
        if (characterNamePanel != null)
            characterNamePanel.SetActive(true);

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        lines.Clear();

        foreach (DialogueLine dialogueLine in dialogue.dialogueLines)
        {
            lines.Enqueue(dialogueLine);
        }

        DisplayNextDialogueLine();
    }

    public void DisplayNextDialogueLine()
    {
        if (isTyping)
        {
            StopAllCoroutines();
            dialogueArea.text = currentFullLine;
            isTyping = false;
            return;
        }

        if (lines.Count == 0)
        {
            EndDialogue();
            return;
        }

        DialogueLine currentLine = lines.Dequeue();

        if (currentLine.character != null)
        {
            //characterIcon.sprite = currentLine.character.icon;
            characterName.text = currentLine.character.name;
        }

        StopAllCoroutines();
        StartCoroutine(TypeSentence(currentLine.line));
    }

    private IEnumerator TypeSentence(string line)
    {
        isTyping = true;
        currentFullLine = line;
        dialogueArea.text = "";

        foreach (char letter in line)
        {
            dialogueArea.text += letter;
            yield return new WaitForSecondsRealtime(typingSpeed);
        }

        isTyping = false;
    }

    private void EndDialogue()
    {
        isDialogueActive = false;
        //animator.Play("hide");

        if (characterNamePanel != null)
            characterNamePanel.SetActive(false); 

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        Action finishedCallback = onDialogueFinished;
        onDialogueFinished = null;
        finishedCallback?.Invoke();
    }
}
