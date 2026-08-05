using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkyDropInstaller : MonoBehaviour
{
    [Header("낙하 연출 설정")]
    [SerializeField] private float dropHeight = 25f;        // 낙하 시작 높이
    [SerializeField] private float dropDuration = 0.6f;      // 개별 오브젝트 낙하 시간
    [SerializeField] private float staggerDelay = 0.15f;     // 오브젝트 간 낙하 시차 간격

    [Header("이펙트 설정")]
    [SerializeField] private ParticleSystem impactVFXPrefab; // 땅에 닿을 때 먼지 파티클
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip impactSFX;

    /// <summary>
    /// 건물과 연관 프롭들을 순차적으로 하늘에서 떨어뜨리는 연출 코루틴
    /// </summary>
    public IEnumerator PlayDropSequenceCoroutine(List<GameObject> spawnList, List<Transform> targetSockets, System.Action onComplete)
    {
        for (int i = 0; i < spawnList.Count; i++)
        {
            if (i >= targetSockets.Count) break;

            GameObject obj = spawnList[i];
            Transform socket = targetSockets[i];

            // 1. 초기 위치 설정 (하늘 위)
            Vector3 targetPos = socket.position;
            Vector3 startPos = targetPos + Vector3.up * dropHeight;
            obj.transform.position = startPos;
            obj.transform.rotation = socket.rotation;
            obj.SetActive(true);

            // 2. 단일 오브젝트 낙하 실행
            StartCoroutine(AnimateSingleObjectDrop(obj, startPos, targetPos));

            // 후두둑 떨어지는 느낌을 위한 시차 부여
            yield return new WaitForSeconds(staggerDelay);
        }

        // 전체 낙하 완료 대기
        yield return new WaitForSeconds(dropDuration);
        onComplete?.Invoke();
    }

    private IEnumerator AnimateSingleObjectDrop(GameObject obj, Vector3 start, Vector3 target)
    {
        float timer = 0f;

        while (timer < dropDuration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / dropDuration);

            // Ease-Out Bounce 공식을 적용한 Y축 위치 계산
            float bounceY = Mathf.Abs(Mathf.Cos(progress * Mathf.PI * 2.5f)) * (1f - progress) * 3f;
            Vector3 currentPos = Vector3.Lerp(start, target, progress);
            currentPos.y += bounceY;

            if (obj != null) obj.transform.position = currentPos;
            yield return null;
        }

        if (obj != null)
        {
            obj.transform.position = target;

            // 착지 먼지 이펙트 및 사운드 재생
            if (impactVFXPrefab != null)
            {
                Instantiate(impactVFXPrefab, target, Quaternion.identity);
            }
            if (audioSource != null && impactSFX != null)
            {
                audioSource.PlayOneShot(impactSFX);
            }
        }
    }
}