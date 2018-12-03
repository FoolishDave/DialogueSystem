using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharaInfo", menuName = "Dialogue/Info", order = 1)]
public class CharacterDialogueInfo : ScriptableObject
{
    public string speakingName;
    public Color color;
    public Sprite sprite;
    public AudioClip sound;
}
