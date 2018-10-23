using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class TextBoxController : MonoBehaviour {
    public float typewriterTime;

    public TextMeshProUGUI textBoxDialogue;
    public TextMeshProUGUI textBoxChar;
    public Image charImg;
    public bool waiting;
    public AudioSource audioSource;
    private bool skip;

    public void Display(DialogueElement element)
    {
        textBoxChar.text = element.speakingName;
        textBoxChar.color = element.speakingColor;
        charImg.sprite = element.speakingSprite;
        StartCoroutine(TypewriterText(element));
    }

    public void Skip()
    {
        skip = true;
    }

    private IEnumerator TypewriterText(DialogueElement element)
    {
        waiting = false;
        textBoxDialogue.text = "";

        char[] dialogueCharArr = element.speakingDialogue.ToCharArray();
        bool displaying = true;
        int index = 0;
        while (displaying && index < dialogueCharArr.Length && !skip)
        {
            textBoxDialogue.text += dialogueCharArr[index];
            if (dialogueCharArr[index] == '<')
            {
                while (dialogueCharArr[index] != '>')
                {
                    index++;
                    textBoxDialogue.text += dialogueCharArr[index];
                }
            }
            if (element.fontSound != null)
            {
                audioSource.pitch = 1 + UnityEngine.Random.Range(-.17f, 0.17f);
                audioSource.PlayOneShot(element.fontSound);
            }
            index++;
            yield return new WaitForSeconds(typewriterTime);
        }
        if (skip) textBoxDialogue.text = element.speakingDialogue;

        skip = false;
        waiting = true;
    }
}

[Serializable]
public class DialogueElement
{
    public enum DialogueType { Standard }

    public DialogueType dialogueType;
    public string speakingName;
    public string speakingDialogue;
    public Color speakingColor;
    public Sprite speakingSprite;
    public AudioClip fontSound;
}
