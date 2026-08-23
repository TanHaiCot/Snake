using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class DialogueCharacter
{
    public string name;
    //public Sprite icon;
}


[System.Serializable]
public class DialogueLine
{
    public DialogueCharacter character;

    [TextArea(3, 10)]
    public string line;
}

[CreateAssetMenu(fileName = "Dialogue", menuName = "Scriptable Objects/Dialogue")]
public class DialogueData : ScriptableObject
{
    public string dialogueID;    //might use to store the dialogue ID for future reference, like saving/loading 
    public List<DialogueLine> dialogueLines = new List<DialogueLine>();
}
