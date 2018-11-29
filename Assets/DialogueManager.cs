using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueManager : MonoBehaviour {

    public List<DialogueElement> dialogue;
    private TextBoxController currentTextBox;
    public GameObject textBoxPrefab;
    public Canvas canvas;

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
        if (Input.GetKeyDown(startKey) && currentTextBox == null)
        {
            currentTextBox = Instantiate(textBoxPrefab, canvas.transform).GetComponent<TextBoxController>();
            currentTextBox.Enqueue(dialogue);
        } 
    }
}
