/*
 * 역할:
 * - EnvironmentSoundHandle을 실제로 반복 호출해보는 테스트 전용 스크립트입니다.
 *
 * 주요 기능:
 * - playOnEnable이 켜져 있으면 Start 이후 환경음 루프를 시작합니다.
 * - loopTime 간격으로 handle.Play()를 호출하여 환경음 API 동작을 검증합니다.
 *
 * 주의:
 * - 실제 게임 로직보다는 Choi_SoundManager_Test Scene의 시연/검증용입니다.
 */
using UnityEngine;

public class Test_SoundPlayEnvironment : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float loopTime = 5.0f;
    [SerializeField] private bool loop = true;
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] private EnvironmentSoundHandle handle;

    private Coroutine loopCoroutine;
    private bool hasStarted;

    private void Awake()
    {
        if (handle == null)
        {
            TryGetComponent(out handle);
        }
    }

    private void OnEnable()
    {
        if (hasStarted && playOnEnable)
        {
            StartLoop();
        }
    }

    private void Start()
    {
        hasStarted = true;

        if (playOnEnable)
        {
            StartLoop();
        }
    }

    private void OnDisable()
    {
        StopLoop();
    }

    public void PlayOnce()
    {
        if (handle == null)
        {
            Debug.LogWarning("EnvironmentSoundHandle is not assigned.");
            return;
        }

        handle.Play();
    }

    public void StartLoop()
    {
        if (!loop || loopCoroutine != null)
        {
            return;
        }

        loopCoroutine = StartCoroutine(LoopRoutine());
    }

    public void StopLoop()
    {
        if (loopCoroutine == null)
        {
            return;
        }

        StopCoroutine(loopCoroutine);
        loopCoroutine = null;
    }

    public void SetLoop(bool isLoop)
    {
        loop = isLoop;

        if (loop)
        {
            StartLoop();
        }
        else
        {
            StopLoop();
        }
    }

    private System.Collections.IEnumerator LoopRoutine()
    {
        while (loop)
        {
            PlayOnce();
            yield return new WaitForSeconds(loopTime);
        }

        loopCoroutine = null;
    }

    private void OnValidate()
    {
        loopTime = Mathf.Max(0.1f, loopTime);
    }
}
