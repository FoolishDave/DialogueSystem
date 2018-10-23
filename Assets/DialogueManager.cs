using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueManager : MonoBehaviour {

    public List<DialogueElement> dialogue;
    public TextBoxController currentTextBox;

    public KeyCode startKey;
    public KeyCode nextKey;
    public KeyCode speedKey;
    public KeyCode skipKey;

    private int dialogueIndex = 0;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        if (Input.GetKeyDown(startKey))
        {
            dialogueIndex = 0;
            currentTextBox.Display(dialogue[dialogueIndex]);
        } else if (Input.GetKeyDown(nextKey) && currentTextBox.waiting)
        {
            dialogueIndex++;
            currentTextBox.Display(dialogue[dialogueIndex]);
        } else if (Input.GetKeyDown(speedKey))
        {
            currentTextBox.typewriterTime /= 4;
        } else if (Input.GetKeyUp(speedKey))
        {
            currentTextBox.typewriterTime *= 4;
        } else if (Input.GetKeyDown(skipKey))
        {
            currentTextBox.Skip();
        }
    }
}
