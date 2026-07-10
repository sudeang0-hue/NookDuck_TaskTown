/*
 * 역할:
 * - 환경음 ID를 가진 오브젝트용 호출 핸들입니다.
 * - 다른 스크립트나 Animation Event가 Play()를 호출하면 EnvironmentSoundRuntime을 통해 환경음을 재생합니다.
 *
 * 주요 기능:
 * - 활성화 시 랜덤 환경음 후보로 등록/해제합니다.
 * - 필요 시 playOnEnable로 활성화 순간 환경음을 한 번 재생합니다.
 */
using UnityEngine;

public class EnvironmentSoundHandle : MonoBehaviour
{
    [SerializeField] private string environmentSoundId;
    [SerializeField] private EnvironmentSoundRuntime environmentSoundRuntime;
    [SerializeField] private bool registerForRandomPlayback = true;
    [SerializeField] private bool playOnEnable;

    private void OnEnable()
    {
        EnvironmentSoundRuntime runtime = GetRuntime();

        if (runtime == null)
        {
            return;
        }

        if (registerForRandomPlayback)
        {
            runtime.RegisterRandomSound(environmentSoundId);
        }

        if (playOnEnable)
        {
            runtime.PlayEnvironment(environmentSoundId);
        }
    }

    private void OnDisable()
    {
        if (!registerForRandomPlayback)
        {
            return;
        }

        EnvironmentSoundRuntime runtime = GetRuntime();

        if (runtime != null)
        {
            runtime.UnregisterRandomSound(environmentSoundId);
        }
    }

    public void Play()
    {
        Play(environmentSoundId);
    }

    public void Play(string soundId)
    {
        EnvironmentSoundRuntime runtime = GetRuntime();

        if (runtime == null)
        {
            Debug.LogWarning("EnvironmentSoundRuntime is not available. Environment sound cannot be played.");
            return;
        }

        runtime.PlayEnvironment(soundId);
    }

    private EnvironmentSoundRuntime GetRuntime()
    {
        return environmentSoundRuntime != null ? environmentSoundRuntime : EnvironmentSoundRuntime.Instance;
    }
}
