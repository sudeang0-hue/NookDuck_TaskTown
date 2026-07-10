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
