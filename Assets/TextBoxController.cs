using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;

public class TextBoxController : MonoBehaviour {

    public enum TextEffect { Shake, Wave, Tremble }
    private string[] customTags = { "shake", "wave", "tremble" };
    private string[] stockTags = { "b", "i", "u", "sup", "sub", "pos", "size", "color" };

    public float typewriterTime;
    public float AngleMultiplier = 1.0f;
    public float SpeedMultiplier = 1.0f;
    public float CurveScale = 1.0f;
    public AnimationCurve WaveCurve = new AnimationCurve();
    public float waveRate = 0.25f;
    public float waveScale = 5f;
    public TextMeshProUGUI textBoxDialogue;
    public TextMeshProUGUI textBoxChar;
    public Image charImg;
    public bool waiting;
    public AudioSource audioSource;
    private bool skip;
    private bool textChanged;
    private List<TagData> tagData = new List<TagData>();
    private List<Coroutine> runningEffects = new List<Coroutine>();

    private struct VertexAnim
    {
        public float angleRange;
        public float angle;
        public float speed;
    }

    private struct TagData
    {
        public TextEffect effect;
        public int startIndex;
        public int endIndex;
    }

    void OnEnable()
    {
        // Subscribe to event fired when text object has been regenerated.
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(ON_TEXT_CHANGED);
    }

    void ON_TEXT_CHANGED(Object obj)
    {
        if (obj == textBoxDialogue)
        {
            textChanged = true;
        }
    }

    public void Display(DialogueElement element)
    {
        textBoxChar.text = element.speakingName;
        textBoxChar.color = element.speakingColor;
        charImg.sprite = element.speakingSprite;
        textBoxDialogue.text = RemoveAndStoreTags(element.speakingDialogue);
        textBoxDialogue.maxVisibleCharacters = 0;
        StopEffects();
        StartTextEffects();
        StartCoroutine(TypewriterText(element));
    }

    public void Skip()
    {
        skip = true;
    }

    private void StartTextEffects()
    {
        foreach (TagData data in tagData)
        {
            switch (data.effect)
            {
                case TextEffect.Shake:
                    runningEffects.Add(StartCoroutine(ShakeText(data.startIndex, data.endIndex)));
                    break;
                case TextEffect.Tremble:
                    break;
                case TextEffect.Wave:
                    runningEffects.Add(StartCoroutine(WaveText(data.startIndex, data.endIndex)));
                    break;
                default:
                    break;
            }
        }
    }

    private void StopEffects()
    {
        runningEffects.ForEach(ef => StopCoroutine(ef));
    }

    private string RemoveAndStoreTags(string text)
    {
        tagData.Clear();
        char[] charArr = text.ToCharArray();
        string sanitized = "";
        int adjustedIndex = 0;
        for (int i = 0; i < text.Length; i++, adjustedIndex++)
        {
            if (charArr[i] == '<')
            {
                int startingIndex = adjustedIndex;
                string tag = "";
                while (charArr[i] != '>')
                {
                    i++;
                    tag += charArr[i];
                }
                tag = tag.Substring(0, tag.Length - 1);
                if (customTags.Contains(tag.ToLower()) && !tag.Contains('/'))
                {
                    TagData tagD = new TagData { effect = (TextEffect)System.Array.IndexOf(customTags, tag.ToLower()), startIndex = startingIndex };
                    tagData.Add(tagD);
                } else if (customTags.Contains(tag.ToLower().Substring(1)) && tag.Contains('/'))
                {
                    tag = tag.Substring(1);
                    TextEffect eff = (TextEffect)System.Array.IndexOf(customTags, tag.ToLower());
                    int listIndex = tagData.FindLastIndex(td => td.effect == eff);
                    TagData data = tagData[listIndex];
                    data.endIndex = startingIndex;
                    tagData[listIndex] = data;
                } else
                {
                    sanitized += "<" + tag + ">";
                    adjustedIndex -= 1;
                    if (tag.Contains('/')) adjustedIndex -= 1;
                }
            } else
            {
                sanitized += charArr[i];
            }
        }
        return sanitized;
    }

    private IEnumerator TypewriterText(DialogueElement element)
    {
        waiting = false;
        bool displaying = true;
        TMP_TextInfo textInfo = textBoxDialogue.textInfo;
        int i = 0;
        while (i < textInfo.characterCount && displaying && !skip)
        {
            textBoxDialogue.maxVisibleCharacters = i;
            if (element.fontSound != null)
            {
                audioSource.pitch = 1 + UnityEngine.Random.Range(-.17f, 0.17f);
                audioSource.PlayOneShot(element.fontSound);
            }
            i++;
            yield return new WaitForSeconds(typewriterTime);
        }
        if (skip)
        {
            textBoxDialogue.maxVisibleCharacters = textInfo.characterCount;
        }

        skip = false;
        waiting = true;
    }

    private IEnumerator WaveText(int startIndex, int endIndex)
    {
        textBoxDialogue.ForceMeshUpdate();
        textChanged = true;
        TMP_TextInfo textInfo = textBoxDialogue.textInfo;
        Matrix4x4 matrix;
        int loopCount = 0;
        float time = 0.0f;

        TMP_MeshInfo[] cachedMeshInfo = textInfo.CopyMeshInfoVertexData();

        while (true)
        {
            if (textChanged)
            {
                cachedMeshInfo = textInfo.CopyMeshInfoVertexData();
                textChanged = false;
            }
            int charCount = textInfo.characterCount;
            if (charCount == 0)
            {
                yield return new WaitForSeconds(typewriterTime);
                continue;
            }

            for (int i = startIndex; i < endIndex; i++)
            {
                TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible)
                    continue;

                // Get the index of the material used by the current character.
                int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;

                // Get the index of the first vertex used by this text element.
                int vertexIndex = textInfo.characterInfo[i].vertexIndex;

                // Get the cached vertices of the mesh used by this text element (character or sprite).
                Vector3[] sourceVertices = cachedMeshInfo[materialIndex].vertices;

                // Determine the center point of each character at the baseline.
                //Vector2 charMidBasline = new Vector2((sourceVertices[vertexIndex + 0].x + sourceVertices[vertexIndex + 2].x) / 2, charInfo.baseLine);
                // Determine the center point of each character.
                Vector2 charMidBasline = (sourceVertices[vertexIndex + 0] + sourceVertices[vertexIndex + 2]) / 2;

                // Need to translate all 4 vertices of each quad to aligned with middle of character / baseline.
                // This is needed so the matrix TRS is applied at the origin for each character.
                Vector3 offset = charMidBasline;

                Vector3[] destinationVertices = textInfo.meshInfo[materialIndex].vertices;

                destinationVertices[vertexIndex + 0] = sourceVertices[vertexIndex + 0] - offset;
                destinationVertices[vertexIndex + 1] = sourceVertices[vertexIndex + 1] - offset;
                destinationVertices[vertexIndex + 2] = sourceVertices[vertexIndex + 2] - offset;
                destinationVertices[vertexIndex + 3] = sourceVertices[vertexIndex + 3] - offset;

                float evalTime = WaveCurve.keys.Last().time * i / (float)(endIndex - startIndex) + time;
                if (evalTime > WaveCurve.keys.Last().time) evalTime = evalTime - WaveCurve.keys.Last().time;
                matrix = Matrix4x4.TRS(new Vector3(0, waveScale*WaveCurve.Evaluate(evalTime), 0), Quaternion.identity, Vector3.one);

                destinationVertices[vertexIndex + 0] = matrix.MultiplyPoint3x4(destinationVertices[vertexIndex + 0]);
                destinationVertices[vertexIndex + 1] = matrix.MultiplyPoint3x4(destinationVertices[vertexIndex + 1]);
                destinationVertices[vertexIndex + 2] = matrix.MultiplyPoint3x4(destinationVertices[vertexIndex + 2]);
                destinationVertices[vertexIndex + 3] = matrix.MultiplyPoint3x4(destinationVertices[vertexIndex + 3]);

                destinationVertices[vertexIndex + 0] += offset;
                destinationVertices[vertexIndex + 1] += offset;
                destinationVertices[vertexIndex + 2] += offset;
                destinationVertices[vertexIndex + 3] += offset;
            }

            // Push changes into meshes
            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
                textBoxDialogue.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
            }
            loopCount += 1;
            time += waveRate;
            if (time > WaveCurve.keys.Last().time - waveRate) time = 0f;
            yield return new WaitForSeconds(0.1f);
            yield return new WaitForEndOfFrame();
        }
    }

    private IEnumerator ShakeText(int startIndex, int endIndex)
    {
        textBoxDialogue.ForceMeshUpdate();
        textChanged = true;
        TMP_TextInfo textInfo = textBoxDialogue.textInfo;
        Matrix4x4 matrix;
        int loopCount = 0;

        VertexAnim[] vertexAnim = new VertexAnim[1024];
        for (int i = 0; i < 1024; i++)
        {
            vertexAnim[i].angleRange = Random.Range(10f, 25f);
            vertexAnim[i].speed = Random.Range(1f, 3f);
        }

        TMP_MeshInfo[] cachedMeshInfo = textInfo.CopyMeshInfoVertexData();

        while (true)
        {
            if (textChanged)
            {
                cachedMeshInfo = textInfo.CopyMeshInfoVertexData();
                textChanged = false;
            }
            int charCount = textInfo.characterCount;
            if (charCount == 0)
            {
                yield return new WaitForSeconds(typewriterTime);
                continue;
            }

            for (int i = startIndex; i < endIndex; i++)
            {
                TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible)
                    continue;

                VertexAnim vertAnim = vertexAnim[i];

                // Get the index of the material used by the current character.
                int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;

                // Get the index of the first vertex used by this text element.
                int vertexIndex = textInfo.characterInfo[i].vertexIndex;

                // Get the cached vertices of the mesh used by this text element (character or sprite).
                Vector3[] sourceVertices = cachedMeshInfo[materialIndex].vertices;

                // Determine the center point of each character at the baseline.
                //Vector2 charMidBasline = new Vector2((sourceVertices[vertexIndex + 0].x + sourceVertices[vertexIndex + 2].x) / 2, charInfo.baseLine);
                // Determine the center point of each character.
                Vector2 charMidBasline = (sourceVertices[vertexIndex + 0] + sourceVertices[vertexIndex + 2]) / 2;

                // Need to translate all 4 vertices of each quad to aligned with middle of character / baseline.
                // This is needed so the matrix TRS is applied at the origin for each character.
                Vector3 offset = charMidBasline;

                Vector3[] destinationVertices = textInfo.meshInfo[materialIndex].vertices;

                destinationVertices[vertexIndex + 0] = sourceVertices[vertexIndex + 0] - offset;
                destinationVertices[vertexIndex + 1] = sourceVertices[vertexIndex + 1] - offset;
                destinationVertices[vertexIndex + 2] = sourceVertices[vertexIndex + 2] - offset;
                destinationVertices[vertexIndex + 3] = sourceVertices[vertexIndex + 3] - offset;

                vertAnim.angle = Mathf.SmoothStep(-vertAnim.angleRange, vertAnim.angleRange, Mathf.PingPong(loopCount / 25f * vertAnim.speed, 1f));
                Vector3 jitterOffset = new Vector3(Random.Range(-.25f, .25f), Random.Range(-.25f, .25f), 0);

                matrix = Matrix4x4.TRS(jitterOffset * CurveScale, Quaternion.Euler(0, 0, Random.Range(-5f, 5f) * AngleMultiplier), Vector3.one);

                destinationVertices[vertexIndex + 0] = matrix.MultiplyPoint3x4(destinationVertices[vertexIndex + 0]);
                destinationVertices[vertexIndex + 1] = matrix.MultiplyPoint3x4(destinationVertices[vertexIndex + 1]);
                destinationVertices[vertexIndex + 2] = matrix.MultiplyPoint3x4(destinationVertices[vertexIndex + 2]);
                destinationVertices[vertexIndex + 3] = matrix.MultiplyPoint3x4(destinationVertices[vertexIndex + 3]);

                destinationVertices[vertexIndex + 0] += offset;
                destinationVertices[vertexIndex + 1] += offset;
                destinationVertices[vertexIndex + 2] += offset;
                destinationVertices[vertexIndex + 3] += offset;

                vertexAnim[i] = vertAnim;
            }

            // Push changes into meshes
            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
                textBoxDialogue.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
            }
            loopCount += 1;

            yield return new WaitForSeconds(0.1f);
            yield return new WaitForEndOfFrame();
        }
    }
}

[System.Serializable]
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
