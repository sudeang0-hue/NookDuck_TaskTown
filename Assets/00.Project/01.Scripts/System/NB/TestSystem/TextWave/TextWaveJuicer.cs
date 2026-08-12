//NB

using System.Collections;
using TMPro;
using UnityEngine;

// TextMeshPro 정점 연산을 활용하여 특정 문자를 제외하고 피아노 파도타기 점프 연출을 수행합니다.
[RequireComponent(typeof(TMP_Text))]
public class TextWaveJuicer : MonoBehaviour
{
    private TMP_Text tmpText;

    [Header("파도타기(Piano Wave) 연출 설정")]
    [Tooltip("글자가 튀어오를 최대 높이 (Pixel)")]
    [SerializeField] private float jumpHeight = 25f;

    [Tooltip("글자 한 개가 완전히 튀어 올랐다 내려오는 시간 (초)")]
    [SerializeField] private float singleJumpDuration = 0.25f;

    [Tooltip("글자 간 파도타기 지연 시간 $\\tau$ (초)")]
    [SerializeField] private float charDelay = 0.04f;

    [Header("예외 처리 설정")]
    [Tooltip("점프 연출에서 제외할 문자 목록 (예: 쉼표, 마침표)")]
    [SerializeField] private string ignoreCharacters = ",.";

    private Coroutine waveCoroutine;

    private void Awake()
    {
        tmpText = GetComponent<TMP_Text>();
    }

    [ContextMenu("Play Test Wave")]
    public void PlayPianoWave()
    {
        if (waveCoroutine != null)
        {
            StopCoroutine(waveCoroutine);
        }

        waveCoroutine = StartCoroutine(AnimatePianoWaveRoutine());
    }

    private IEnumerator AnimatePianoWaveRoutine()
    {
        tmpText.ForceMeshUpdate();

        TMP_TextInfo textInfo = tmpText.textInfo;
        int characterCount = textInfo.characterCount;

        if (characterCount == 0) yield break;

        float totalDuration = (characterCount - 1) * charDelay + singleJumpDuration;
        float elapsedTime = 0f;

        while (elapsedTime < totalDuration)
        {
            elapsedTime += Time.deltaTime;
            tmpText.ForceMeshUpdate();

            for (int i = 0; i < characterCount; i++)
            {
                TMP_CharacterInfo charInfo = textInfo.characterInfo[i];

                // 1. 공백, 출력 불가 문자 제외
                if (!charInfo.isVisible) continue;

                // 2. 쉼표(',') 등 예외 문자 목록 포함 여부 검사
                if (ignoreCharacters.Contains(charInfo.character.ToString())) continue;

                // 개별 글자의 파동 시간 연산 ($t_i = t - i \cdot \tau$)
                float charTime = elapsedTime - (i * charDelay);

                if (charTime >= 0f && charTime <= singleJumpDuration)
                {
                    float progress = charTime / singleJumpDuration;
                    float yOffset = Mathf.Sin(progress * Mathf.PI) * jumpHeight;

                    int materialIndex = charInfo.materialReferenceIndex;
                    int vertexIndex = charInfo.vertexIndex;

                    Vector3[] sourceVertices = textInfo.meshInfo[materialIndex].vertices;

                    // 해당 글자의 4개 정점(Quad)에만 Y축 이동 적용
                    sourceVertices[vertexIndex + 0].y += yOffset;
                    sourceVertices[vertexIndex + 1].y += yOffset;
                    sourceVertices[vertexIndex + 2].y += yOffset;
                    sourceVertices[vertexIndex + 3].y += yOffset;
                }
            }

            // GPU 정점 버퍼 업로드
            for (int i = 0; i < textInfo.materialCount; i++)
            {
                textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
                tmpText.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
            }

            yield return null;
        }

        tmpText.ForceMeshUpdate();
    }
}